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

    private List<NoteController> _activeNotes = new List<NoteController>();
    private int _nextNoteIndex = 0;
    private double _songStartTime;
    private List<GameObject> _pool = new List<GameObject>();
    private bool _isGameOver = false; 
    private bool _hasLoggedMusicEnd = false; // 防止日志疯狂刷屏的锁

    void Start()
    {
        _isGameOver = false;
        _hasLoggedMusicEnd = false;

        // 【监控日志】：检查管家有没有挂载
        if (resultManager == null) Debug.LogError("【致命错误】TrackManager 没有关联 ResultManager！结算界面绝对弹不出来！");

        if (GameDataManager.SelectedLevel != null)
        {
            LevelData data = GameDataManager.SelectedLevel;
            if(musicSource != null) musicSource.clip = data.musicClip;
            sheetMusic = new List<NoteData>(data.notes);
            Debug.Log($"【关卡加载】成功加载：{data.levelName}，共 {sheetMusic.Count} 个音符。");
        }

        _songStartTime = AudioSettings.dspTime;
        if (musicSource != null && musicSource.clip != null) musicSource.Play();
        if (judgmentLine != null) judgmentLine.SetBaseColor(playerColorState);
        UpdateUI(); 
    }

    void Update()
    {
        if (_isGameOver) return; 

        float songTime = GetSongTime();
        
        if (_nextNoteIndex < sheetMusic.Count && songTime >= sheetMusic[_nextNoteIndex].time - noteTravelTime)
        {
            SpawnNote(sheetMusic[_nextNoteIndex], _nextNoteIndex);
            _nextNoteIndex++;
        }

        if (Input.GetKeyDown(KeyCode.Q)) ChangePlayerColor(0);
        if (Input.GetKeyDown(KeyCode.W)) ChangePlayerColor(1);
        if (Input.GetKeyDown(KeyCode.E)) ChangePlayerColor(2);
        if (Input.GetKeyDown(KeyCode.Space)) HandleHitInput();

        // ==========================================
        // 【核心监控区】：检测游戏是否结束
        // ==========================================
        if (musicSource != null && !musicSource.isPlaying && songTime > 2f)
        {
            if (!_hasLoggedMusicEnd)
            {
                Debug.LogWarning($"【结算检测】音乐已经停止！当前进度 -> 发牌数:{_nextNoteIndex}/{sheetMusic.Count} | 屏幕残留音符数:{_activeNotes.Count}");
                _hasLoggedMusicEnd = true; // 确保这句只打印一次
            }

            // 只有当所有音符都发完了，且屏幕上没有残留音符时，才触发结算
            if (_nextNoteIndex >= sheetMusic.Count)
            {
                if (_activeNotes.Count == 0)
                {
                    Debug.Log("【结算检测】完美！所有音符处理完毕，准备呼叫结算面板...");
                    TriggerGameOver();
                }
                else
                {
                    // 【保险机制】：如果卡了 3 秒还是有幽灵音符，强制结算！
                    if (songTime > musicSource.clip.length + 3f)
                    {
                        Debug.LogError($"【结算异常】发现 {_activeNotes.Count} 个幽灵音符没有被回收！不管了，强制触发结算！");
                        TriggerGameOver();
                    }
                }
            }
        }
    }

    private void TriggerGameOver()
    {
        _isGameOver = true;
        Debug.Log($"【执行结算】最终得分:{currentScore}, 最高连击:{maxCombo}, 漏接:{missCount}");
        
        if (resultManager != null)
        {
            LevelData nextLv = GameDataManager.SelectedLevel != null ? GameDataManager.SelectedLevel.nextLevel : null;
            resultManager.ShowResult(currentScore, maxCombo, missCount, nextLv);
        }
        else
        {
            Debug.LogError("【执行失败】TriggerGameOver 被调用了，但是 resultManager 是空的！");
        }
    }

    // ========== 下方战斗判定逻辑完全不变 ==========
    public float GetSongTime() => (float)(AudioSettings.dspTime - _songStartTime);
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
            string judgeText = ""; Color judgeColor = Color.white;
            switch (type)
            {
                case JudgmentType.Perfect: judgeText = "PERFECT"; judgeColor = new Color(1f, 0.85f, 0f); break;
                case JudgmentType.Good: judgeText = "GOOD"; judgeColor = Color.green; break;
                case JudgmentType.Miss: judgeText = "MISS"; judgeColor = Color.gray; TriggerCameraShake(); break;
            }
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
    
    // 给音符脚本调用的接口
    public void HandlePassiveMiss(NoteController note) 
    { 
        if (_activeNotes.Contains(note)) ExecuteJudgment(JudgmentType.Miss, note, true); 
    }
    
    void SpawnNote(NoteData data, int index) { GameObject obj = GetPooledNote(); obj.SetActive(true); NoteController nc = obj.GetComponent<NoteController>(); nc.Initialize(index, data.time, noteTravelTime, spawnPoint.position, targetPoint.position, data.type); _activeNotes.Add(nc); }
    public GameObject GetPooledNote() { for (int i = 0; i < _pool.Count; i++) if (_pool[i] != null && !_pool[i].activeInHierarchy) return _pool[i]; GameObject newNote = Instantiate(notePrefab); _pool.Add(newNote); return newNote; }
}