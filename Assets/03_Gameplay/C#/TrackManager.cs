using UnityEngine;
using System.Collections.Generic;
using TMPro;

// ==========================================
// 【全局定义】：确保判定线和音符都能识别
// ==========================================
public enum JudgmentType
{
    Perfect,
    Good,
    Miss
}

[System.Serializable]
public class NoteData 
{
    public float time; // 击中时间
    public int type;   // 0:红, 1:白, 2:蓝
}

// ==========================================
// 【核心逻辑类】
// ==========================================
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
    public JudgmentDisplay judgmentDisplayPrefab; // 判定文字预制体
    public Transform judgmentParent;               // 判定文字的父容器 (JudgmentContainer)
    public Vector2 randomRange = new Vector2(50f, 30f);

    [Header("积分数值")]
    public int scorePerPerfect = 1000;
    public int scorePerGood = 500;

    [Header("实时统计")]
    public int currentScore = 0;
    public int currentCombo = 0;
    public int maxCombo = 0;
    [HideInInspector] public int perfectCount, goodCount, missCount;

    [Header("判定阈值 (秒)")]
    public float perfectThreshold = 0.05f; 
    public float goodThreshold = 0.15f;    

    [Header("轨道配置")]
    public int playerColorState = 1; 
    public float noteTravelTime = 2.0f;
    public List<NoteData> sheetMusic = new List<NoteData>();

    private List<NoteController> _activeNotes = new List<NoteController>();
    private int _nextNoteIndex = 0;
    private double _songStartTime;
    private List<GameObject> _pool = new List<GameObject>();

    void Start()
    {
        _songStartTime = AudioSettings.dspTime;
        if (musicSource != null) musicSource.Play();
        if (judgmentLine != null) judgmentLine.SetBaseColor(playerColorState);
        UpdateUI(); 
    }

    void Update()
    {
        float songTime = GetSongTime();
        
        // 1. 自动生成音符
        if (_nextNoteIndex < sheetMusic.Count && songTime >= sheetMusic[_nextNoteIndex].time - noteTravelTime)
        {
            SpawnNote(sheetMusic[_nextNoteIndex], _nextNoteIndex);
            _nextNoteIndex++;
        }

        // 2. 输入监听
        if (Input.GetKeyDown(KeyCode.Q)) ChangePlayerColor(0);
        if (Input.GetKeyDown(KeyCode.W)) ChangePlayerColor(1);
        if (Input.GetKeyDown(KeyCode.E)) ChangePlayerColor(2);
        if (Input.GetKeyDown(KeyCode.Space)) HandleHitInput();
    }

    public float GetSongTime() => (float)(AudioSettings.dspTime - _songStartTime);

    public void ChangePlayerColor(int newColor)
    {
        playerColorState = newColor;
        if (judgmentLine != null) judgmentLine.SetBaseColor(newColor);
    }

    // 处理击打输入
    public void HandleHitInput()
    {
        // 情况 A：空挥 (附近没有音符)
        if (_activeNotes.Count == 0 || Mathf.Abs(GetSongTime() - _activeNotes[0].hitTime) > 0.3f) 
        {
            TriggerFeedbackAction(targetPoint.position, Color.clear);
            TriggerCameraShake(); 
            SpawnJudgmentText("MISS", Color.gray); // 标准 MISS
            ResetCombo(); 
            return;
        }

        // 情况 B：正常判定
        NoteController targetNote = _activeNotes[0];
        float diff = Mathf.Abs(GetSongTime() - targetNote.hitTime);

        if (targetNote.noteType == playerColorState) 
        {
            if (diff <= perfectThreshold) ExecuteJudgment(JudgmentType.Perfect, targetNote);
            else if (diff <= goodThreshold) ExecuteJudgment(JudgmentType.Good, targetNote);
            else ExecuteJudgment(JudgmentType.Miss, targetNote); 
        }
        else
        {
            ExecuteJudgment(JudgmentType.Miss, targetNote);
        }
    }

    // 核心判定执行出口
    private void ExecuteJudgment(JudgmentType type, NoteController note, bool isPassive = false)
    {
        // 1. 数值逻辑处理
        switch (type)
        {
            case JudgmentType.Perfect:
                perfectCount++;
                currentScore += scorePerPerfect;
                AddCombo();
                break;
            case JudgmentType.Good:
                goodCount++;
                currentScore += scorePerGood;
                AddCombo();
                break;
            case JudgmentType.Miss:
                missCount++;
                ResetCombo();
                break;
        }

        if (judgmentLine != null) judgmentLine.ApplyJudgment(type);

        // 2. 视觉表现处理 (主动击打时触发)
        if (!isPassive) 
        {
            string judgeText = "";
            Color judgeColor = Color.white;

            switch (type)
            {
                case JudgmentType.Perfect: 
                    judgeText = "PERFECT"; 
                    judgeColor = new Color(1f, 0.85f, 0f); // 金色
                    break;
                case JudgmentType.Good: 
                    judgeText = "GOOD"; 
                    judgeColor = Color.green; // 绿色
                    break;
                case JudgmentType.Miss: 
                    judgeText = "MISS"; 
                    judgeColor = Color.gray; // 灰色
                    TriggerCameraShake(); 
                    break;
            }

            // 弹出文字
            SpawnJudgmentText(judgeText, judgeColor);

            // 弹飞音符
            Color flyColor = Color.clear;
            if (type != JudgmentType.Miss)
            {
                SpriteRenderer sr = note.GetComponent<SpriteRenderer>();
                if (sr != null) flyColor = sr.color; 
            }
            TriggerFeedbackAction(note.transform.position, flyColor);
        }

        UpdateUI(); 

        // 3. 回收音符
        if (_activeNotes.Contains(note)) _activeNotes.Remove(note);
        note.Deactivate(); 
    }

    // --- 功能辅助函数 ---

    private void AddCombo()
    {
        currentCombo++;
        if (currentCombo > maxCombo) maxCombo = currentCombo;
    }

    private void ResetCombo()
    {
        currentCombo = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"SCORE: {currentScore}";
        if (comboText != null) 
        {
            comboText.text = currentCombo > 0 ? $"{currentCombo} COMBO" : "";
        }
    }

    private void SpawnJudgmentText(string text, Color color)
    {
        if (judgmentDisplayPrefab != null && judgmentParent != null)
        {
            JudgmentDisplay go = Instantiate(judgmentDisplayPrefab, judgmentParent);
            Vector2 offset = new Vector2(
                Random.Range(-randomRange.x, randomRange.x), 
                Random.Range(-randomRange.y, randomRange.y)
            );
            go.Init(text, color, offset);
        }
    }

    private void TriggerFeedbackAction(Vector3 pos, Color color) 
    { 
        var f = FindObjectOfType<HitFeedbackManager>(); 
        if (f != null) f.TriggerHitFeedback(pos, color); 
    }

    private void TriggerCameraShake() 
    { 
        var f = FindObjectOfType<HitFeedbackManager>(); 
        if (f != null) f.TriggerMissShake(); 
    }

    public void HandlePassiveMiss(NoteController note) 
    { 
        if (_activeNotes.Contains(note)) ExecuteJudgment(JudgmentType.Miss, note, true); 
    }
    
    void SpawnNote(NoteData data, int index)
    {
        GameObject obj = GetPooledNote();
        obj.SetActive(true);
        NoteController nc = obj.GetComponent<NoteController>();
        nc.Initialize(index, data.time, noteTravelTime, spawnPoint.position, targetPoint.position, data.type);
        _activeNotes.Add(nc);
    }

    public GameObject GetPooledNote() 
    {
        for (int i = 0; i < _pool.Count; i++) 
        {
            if (_pool[i] != null && !_pool[i].activeInHierarchy) return _pool[i];
        }
        GameObject newNote = Instantiate(notePrefab);
        _pool.Add(newNote); 
        return newNote;
    }
}