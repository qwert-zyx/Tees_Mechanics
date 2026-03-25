using UnityEngine;
using System.Collections.Generic;

public enum JudgmentType
{
    Perfect,
    Good,
    Miss
}

[System.Serializable]
public class NoteData {
    public float time; // 击中时间
    public int type;   // 0:红, 1:白, 2:蓝
}

public class TrackManager : MonoBehaviour
{
    [Header("核心引用")]
    public Transform spawnPoint;
    public Transform targetPoint;
    public AudioSource musicSource;
    public GameObject notePrefab;
    public JudgmentLineController judgmentLine;

    [Header("判定文字设置 (工厂模式)")]
    public GameObject judgmentPrefab;  // 拖入 Project 里的判定文字 Prefab
    public Transform judgmentParent;    // 拖入场景里的 Canvas
    public Vector2 randomRange = new Vector2(50f, 30f); // 弹出位置的随机偏移量

    [Header("判定阈值 (秒)")]
    public float perfectThreshold = 0.05f; 
    public float goodThreshold = 0.15f;    

    [Header("实时统计")]
    public int perfectCount = 0;
    public int goodCount = 0;
    public int missCount = 0;

    [Header("玩家当前颜色状态")]
    public int playerColorState = 1; // 0:红, 1:白, 2:蓝

    [Header("轨道配置")]
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
        
        // 初始化判定线颜色
        if (judgmentLine != null) judgmentLine.SetBaseColor(playerColorState);
    }

    public float GetSongTime() => (float)(AudioSettings.dspTime - _songStartTime);

    void Update()
    {
        float songTime = GetSongTime();

        // 1. 自动生成音符逻辑
        if (_nextNoteIndex < sheetMusic.Count && songTime >= sheetMusic[_nextNoteIndex].time - noteTravelTime)
        {
            SpawnNote(sheetMusic[_nextNoteIndex], _nextNoteIndex);
            _nextNoteIndex++;
        }

        // 2. 切换颜色状态输入
        if (Input.GetKeyDown(KeyCode.Q)) ChangePlayerColor(0);
        if (Input.GetKeyDown(KeyCode.W)) ChangePlayerColor(1);
        if (Input.GetKeyDown(KeyCode.E)) ChangePlayerColor(2);

        // 3. 击打判定输入 (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleHitInput();
        }
    }

    // 切换颜色逻辑
    public void ChangePlayerColor(int newColor)
    {
        playerColorState = newColor;
        if (judgmentLine != null) judgmentLine.SetBaseColor(newColor);
    }

    // 处理主动击打
    public void HandleHitInput()
    {
        // 情况 A：空挥（轨道没音符，或最近的音符太远）
        if (_activeNotes.Count == 0 || Mathf.Abs(GetSongTime() - _activeNotes[0].hitTime) > 0.3f) 
        {
            TriggerFeedbackAction(targetPoint.position, Color.clear);
            TriggerCameraShake(); 
            SpawnJudgmentText("MISS", Color.gray);
            return;
        }

        // 情况 B：正常判定区域内有音符
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
            // 颜色对不上，直接判定为 Miss
            ExecuteJudgment(JudgmentType.Miss, targetNote);
        }
    }

    // 统一判定处理出口
    private void ExecuteJudgment(JudgmentType type, NoteController note, bool isPassive = false)
    {
        // 1. 统计数据
        switch (type)
        {
            case JudgmentType.Perfect: perfectCount++; break;
            case JudgmentType.Good: goodCount++; break;
            case JudgmentType.Miss: missCount++; break;
        }

        if (judgmentLine != null) judgmentLine.ApplyJudgment(type);

        // 2. 只有非被动（主动击打）才触发反馈
        if (!isPassive) 
        {
            string judgeText = "";
            Color judgeColor = Color.white;

            switch (type)
            {
                case JudgmentType.Perfect: 
                    judgeText = "PERFECT"; 
                    judgeColor = new Color(1f, 0.85f, 0f); 
                    break;
                case JudgmentType.Good: 
                    judgeText = "GOOD"; 
                    judgeColor = Color.green; 
                    break;
                case JudgmentType.Miss: 
                    judgeText = "MISS"; 
                    judgeColor = Color.gray; 
                    TriggerCameraShake(); // 打错时震屏
                    break;
            }

            // 弹出判定文字
            SpawnJudgmentText(judgeText, judgeColor);

            // 处理音符弹飞颜色
            Color flyColor = Color.clear;
            if (type == JudgmentType.Perfect || type == JudgmentType.Good)
            {
                SpriteRenderer sr = note.GetComponent<SpriteRenderer>();
                if (sr != null) flyColor = sr.color; 
            }

            TriggerFeedbackAction(note.transform.position, flyColor);
        }

        // 3. 移除并回收
        if (_activeNotes.Contains(note)) _activeNotes.Remove(note);
        note.Deactivate(); 
    }

    // 工厂函数：生成判定文字
    private void SpawnJudgmentText(string text, Color color)
    {
        if (judgmentPrefab != null && judgmentParent != null)
        {
            GameObject go = Instantiate(judgmentPrefab, judgmentParent);
            
            // 随机位移偏移
            Vector2 offset = new Vector2(
                Random.Range(-randomRange.x, randomRange.x),
                Random.Range(-randomRange.y, randomRange.y)
            );

            var display = go.GetComponent<JudgmentDisplay>();
            if(display != null) display.Init(text, color, offset);
        }
    }

    // 助手方法
    private void TriggerFeedbackAction(Vector3 pos, Color color)
    {
        var feedback = FindObjectOfType<HitFeedbackManager>();
        if (feedback != null) feedback.TriggerHitFeedback(pos, color);
    }

    private void TriggerCameraShake()
    {
        var feedback = FindObjectOfType<HitFeedbackManager>();
        if (feedback != null) feedback.TriggerMissShake();
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