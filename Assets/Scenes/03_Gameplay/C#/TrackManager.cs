using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum JudgmentType { Perfect, Good, Miss }

public class TrackManager : MonoBehaviour
{
    [Header("核心引用")]
    public Transform spawnPoint;
    public Transform targetPoint;
    public AudioSource musicSource;
    public GameObject notePrefab;
    public JudgmentLineController judgmentLine;

    [Header("UI 引用")]
    public TextMeshProUGUI scoreText; 
    public TextMeshProUGUI comboText; 
    public JudgmentDisplay judgmentDisplayPrefab; 
    public Transform judgmentParent;               
    public Vector2 randomRange = new Vector2(50f, 30f);
    
    [Header("结算模块")]
    public ResultManager resultManager; 

    [Header("积分数值")]
    public int scorePerPerfect = 1000;
    public int scorePerGood = 500;

    [Header("实时统计")]
    public int currentScore = 0;
    public int currentCombo = 0;
    public int maxCombo = 0;
    [HideInInspector] public int perfectCount, goodCount, missCount;

    [Header("判定阈值")]
    public float perfectThreshold = 0.05f; 
    public float goodThreshold = 0.15f;    
    public int playerColorState = 1; 
    public float noteTravelTime = 2.0f;
    
    public List<NoteData> sheetMusic = new List<NoteData>();

    [Header("运行数据 (私有)")]
    private List<NoteController> _activeNotes = new List<NoteController>();
    private int _nextNoteIndex = 0;
    private double _songStartTime;
    private List<GameObject> _pool = new List<GameObject>();
    private bool _isGameOver = false; 
    private bool _hasLoggedMusicEnd = false; 

    // ==========================================
    // 【核心新增】：暂停与恢复系统相关的变量
    // ==========================================
    [HideInInspector] public bool isPaused = false;
    private double _pauseStartTime;

    void Start()
    {
        _isGameOver = false;
        _hasLoggedMusicEnd = false;
        isPaused = false; // 初始确保不暂停

        if (resultManager == null) Debug.LogError("【致命错误】TrackManager 没有关联 ResultManager！结算界面绝对弹不出来！");

        if (GameDataManager.SelectedLevel != null)
        {
            LevelData data = GameDataManager.SelectedLevel;
            if(musicSource != null) musicSource.clip = data.musicClip;
            sheetMusic = new List<NoteData>(data.notes);
            Debug.Log($"【关卡加载】成功加载：{data.name}，共 {sheetMusic.Count} 个音符。");
        }

        _songStartTime = AudioSettings.dspTime;
        if (musicSource != null && musicSource.clip != null) musicSource.Play();
        if (judgmentLine != null) judgmentLine.SetBaseColor(playerColorState);
        UpdateUI(); 
    }

    void Update()
    {
        // 1. 如果游戏结束或处于暂停状态，停止所有逻辑更新
        if (_isGameOver || isPaused) return; 

        float songTime = GetSongTime();
        
        // 2. 音符生成逻辑
        if (_nextNoteIndex < sheetMusic.Count && songTime >= sheetMusic[_nextNoteIndex].time - noteTravelTime)
        {
            SpawnNote(sheetMusic[_nextNoteIndex], _nextNoteIndex);
            _nextNoteIndex++;
        }

        // 3. 输入检测
        if (Input.GetKeyDown(KeyCode.Q)) ChangePlayerColor(0);
        if (Input.GetKeyDown(KeyCode.W)) ChangePlayerColor(1);
        if (Input.GetKeyDown(KeyCode.E)) ChangePlayerColor(2);
        if (Input.GetKeyDown(KeyCode.Space)) HandleHitInput();

        // 4. 结算检测
        CheckSongEnd(songTime);
    }

    // ==========================================
    // 【核心新增】：暂停、恢复与时间补偿逻辑
    // ==========================================
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;
        if (musicSource != null) musicSource.Pause(); 
        _pauseStartTime = AudioSettings.dspTime;      
        Debug.Log("<color=cyan>【教程】游戏已暂停</color>");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        if (musicSource != null) musicSource.Play();  
        // 补偿暂停流逝的时间，让歌曲进度和打击时间依然对齐
        _songStartTime += (AudioSettings.dspTime - _pauseStartTime); 
        Debug.Log("<color=cyan>【教程】游戏已恢复</color>");
    }

    // 升级版的时间获取：暂停时返回冻结的时间点
    public float GetSongTime() 
    { 
        if (isPaused) return (float)(_pauseStartTime - _songStartTime);
        return (float)(AudioSettings.dspTime - _songStartTime); 
    }

    // 给引导管家提供情报的两个检测雷达
    public bool IsNextNoteInHitZone(float threshold = 0.15f)
    {
        if (_activeNotes.Count == 0) return false;
        // 检查最近的一个音符是否进入了判定范围
        return Mathf.Abs(GetSongTime() - _activeNotes[0].hitTime) <= threshold;
    }
    public int GetActiveNoteCount() => _activeNotes.Count;


    // ==========================================
    // 战斗判定与 UI 逻辑 (保持不变)
    // ==========================================
    
    private void CheckSongEnd(float songTime)
    {
        if (musicSource != null && !musicSource.isPlaying && songTime > 2f)
        {
            if (!_hasLoggedMusicEnd)
            {
                Debug.LogWarning($"【结算检测】音乐已经停止！发牌:{_nextNoteIndex}/{sheetMusic.Count} | 屏幕残留:{_activeNotes.Count}");
                _hasLoggedMusicEnd = true;
            }

            if (_nextNoteIndex >= sheetMusic.Count)
            {
                if (_activeNotes.Count == 0) TriggerGameOver();
                else if (songTime > musicSource.clip.length + 3f) TriggerGameOver();
            }
        }
    }

    private void TriggerGameOver()
    {
        _isGameOver = true;
        if (resultManager != null)
        {
            LevelData nextLv = GameDataManager.SelectedLevel != null ? GameDataManager.SelectedLevel.nextLevel : null;
            resultManager.ShowResult(currentScore, maxCombo, missCount, nextLv);
        }
    }

    public void ChangePlayerColor(int newColor) { playerColorState = newColor; if (judgmentLine != null) judgmentLine.SetBaseColor(newColor); }
    
    public void HandleHitInput()
    {
        if (_activeNotes.Count == 0 || Mathf.Abs(GetSongTime() - _activeNotes[0].hitTime) > 0.3f) 
        {
            TriggerFeedbackAction(targetPoint.position, Color.clear);
            TriggerCameraShake(); SpawnJudgmentText("MISS", Color.gray); ResetCombo(); 
            missCount++; 
            return;
        }

        NoteController targetNote = _activeNotes[0];
        float diff = Mathf.Abs(GetSongTime() - targetNote.hitTime);

        if (targetNote.noteType == playerColorState) 
        {
            if (diff <= perfectThreshold) ExecuteJudgment(JudgmentType.Perfect, targetNote);
            else if (diff <= goodThreshold) ExecuteJudgment(JudgmentType.Good, targetNote);
            else ExecuteJudgment(JudgmentType.Miss, targetNote); 
        }
        else ExecuteJudgment(JudgmentType.Miss, targetNote);
    }

    private void ExecuteJudgment(JudgmentType type, NoteController note, bool isPassive = false)
    {
        switch (type)
        {
            case JudgmentType.Perfect: perfectCount++; currentScore += scorePerPerfect; AddCombo(); break;
            case JudgmentType.Good: goodCount++; currentScore += scorePerGood; AddCombo(); break;
            case JudgmentType.Miss: missCount++; ResetCombo(); break;
        }
        if (judgmentLine != null) judgmentLine.ApplyJudgment(type);
        if (!isPassive) 
        {
            string judgeText = (type == JudgmentType.Perfect) ? "PERFECT" : (type == JudgmentType.Good ? "GOOD" : "MISS");
            Color judgeColor = (type == JudgmentType.Perfect) ? new Color(1f, 0.85f, 0f) : (type == JudgmentType.Good ? Color.green : Color.gray);
            SpawnJudgmentText(judgeText, judgeColor);
            Color flyColor = Color.clear;
            if (type != JudgmentType.Miss) { SpriteRenderer sr = note.GetComponent<SpriteRenderer>(); if (sr != null) flyColor = sr.color; }
            TriggerFeedbackAction(note.transform.position, flyColor);
        }
        UpdateUI(); 
        if (_activeNotes.Contains(note)) _activeNotes.Remove(note);
        note.Deactivate(); 
    }

    private void AddCombo() { currentCombo++; if (currentCombo > maxCombo) maxCombo = currentCombo; }
    private void ResetCombo() { currentCombo = 0; UpdateUI(); }
    private void UpdateUI() { if (scoreText != null) scoreText.text = $"SCORE: {currentScore:N0}"; if (comboText != null) comboText.text = currentCombo > 0 ? $"{currentCombo} COMBO" : ""; }
    private void SpawnJudgmentText(string text, Color color) { if (judgmentDisplayPrefab != null && judgmentParent != null) { JudgmentDisplay go = Instantiate(judgmentDisplayPrefab, judgmentParent); Vector2 offset = new Vector2(Random.Range(-randomRange.x, randomRange.x), Random.Range(-randomRange.y, randomRange.y)); go.Init(text, color, offset); } }
    private void TriggerFeedbackAction(Vector3 pos, Color color) { var f = FindObjectOfType<HitFeedbackManager>(); if (f != null) f.TriggerHitFeedback(pos, color); }
    private void TriggerCameraShake() { var f = FindObjectOfType<HitFeedbackManager>(); if (f != null) f.TriggerMissShake(); }
    public void HandlePassiveMiss(NoteController note) { if (_activeNotes.Contains(note)) ExecuteJudgment(JudgmentType.Miss, note, true); }
    void SpawnNote(NoteData data, int index) { GameObject obj = GetPooledNote(); obj.SetActive(true); NoteController nc = obj.GetComponent<NoteController>(); nc.Initialize(index, data.time, noteTravelTime, spawnPoint.position, targetPoint.position, data.type); _activeNotes.Add(nc); }
    public GameObject GetPooledNote() { for (int i = 0; i < _pool.Count; i++) if (_pool[i] != null && !_pool[i].activeInHierarchy) return _pool[i]; GameObject newNote = Instantiate(notePrefab); _pool.Add(newNote); return newNote; }
}