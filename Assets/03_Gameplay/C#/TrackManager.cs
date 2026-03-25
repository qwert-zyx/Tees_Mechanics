using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 1. 全局判定等级枚举 (放在类外面即可全局访问)
// ==========================================
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
    [Header("引用")]
    public Transform spawnPoint;
    public Transform targetPoint;
    public AudioSource musicSource;
    public GameObject notePrefab;
    public JudgmentLineController judgmentLine;

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
        judgmentLine.SetBaseColor(playerColorState);
    }

    // 获取当前歌曲播放的相对时间
    public float GetSongTime() => (float)(AudioSettings.dspTime - _songStartTime);

    void Update()
    {
        float songTime = GetSongTime();

        // 1. 自动生成逻辑
        if (_nextNoteIndex < sheetMusic.Count && songTime >= sheetMusic[_nextNoteIndex].time - noteTravelTime)
        {
            SpawnNote(sheetMusic[_nextNoteIndex], _nextNoteIndex);
            _nextNoteIndex++;
        }

        // 2. 切换颜色状态输入 (QWE)
        if (Input.GetKeyDown(KeyCode.Q)) ChangePlayerColor(0);
        if (Input.GetKeyDown(KeyCode.W)) ChangePlayerColor(1);
        if (Input.GetKeyDown(KeyCode.E)) ChangePlayerColor(2);

        // 3. 击打判定输入 (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleHitInput();
        }
    }

   public void ChangePlayerColor(int newColor)
    {
        playerColorState = newColor;
        judgmentLine.SetBaseColor(newColor);
        Debug.Log($"<color=white>[状态] 切换至模式: {newColor}</color>");
    }

   public void HandleHitInput()
    {
        // 1. 如果轨道上一个音符都没有，或者最近的音符太远（空挥）
        if (_activeNotes.Count == 0 || Mathf.Abs(GetSongTime() - _activeNotes[0].hitTime) > 0.3f) 
        {
            // 触发空挥特效（完全透明的 Color.clear）
            TriggerFeedbackAction(targetPoint.position, Color.clear);
            return;
        }

        // 永远只判定列表中最前面的音符
        NoteController targetNote = _activeNotes[0];
        float diff = Mathf.Abs(GetSongTime() - targetNote.hitTime);

        // 【核心判定流】
        if (targetNote.noteType == playerColorState) // 颜色对了吗？
        {
            if (diff <= perfectThreshold) ExecuteJudgment(JudgmentType.Perfect, targetNote);
            else if (diff <= goodThreshold) ExecuteJudgment(JudgmentType.Good, targetNote);
            else ExecuteJudgment(JudgmentType.Miss, targetNote); // 颜色对但时机太歪
        }
        else
        {
            ExecuteJudgment(JudgmentType.Miss, targetNote);
        }
    }

    // 新增：专门负责呼叫反馈管理器的方法
    private void TriggerFeedbackAction(Vector3 pos, Color color)
    {
        var feedback = FindObjectOfType<HitFeedbackManager>();
        if (feedback != null)
        {
            feedback.TriggerHitFeedback(pos, color);
        }
    }

    // 供 Note 调用的被动 Miss
    public void HandlePassiveMiss(NoteController note)
    {
        if (_activeNotes.Contains(note))
        {
            ExecuteJudgment(JudgmentType.Miss, note);
        }
    }

    // 【核心入口】：统一分发计数、反馈、移除逻辑
    private void ExecuteJudgment(JudgmentType type, NoteController note)
{
    // 1. 原有的统计数据 (Perfect Count++ 等)
    switch (type)
    {
        case JudgmentType.Perfect: perfectCount++; break;
        case JudgmentType.Good: goodCount++; break;
        case JudgmentType.Miss: missCount++; break;
    }

    // 2. 原有的触发判定线变色逻辑 (判定线闪白、闪黄等)
    if (judgmentLine != null) judgmentLine.ApplyJudgment(type);

    // ==========================================
    // 3. 【核心新增：动态获取原生颜色】
    // ==========================================
    // 我们定义一个要飞出去的颜色变量，默认为完全透明 (Color.clear)
    Color flyColor = Color.clear;

    // 只有打准了 (Perfect 或 Good)，才飞特效
    if (type == JudgmentType.Perfect || type == JudgmentType.Good)
    {
        // === [ 原来的写法 (硬编码白色) ] ===
        // flyColor = Color.white; 

        // === [ 现在的写法 (激活预留接口，获取原生颜色) ] ===
        // 我们从被击中的音符身上动态拿到它的 SpriteRenderer，然后读取它的颜色！
        SpriteRenderer sr = note.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 拿到音符本来的颜色 (比如红、白、蓝)
            flyColor = sr.color; 
        }
    }

    // 4. 触发飞出特效 (将动态拿到的颜色传过去)
    TriggerFeedbackAction(note.transform.position, flyColor);

    // 5. 回收音符逻辑
    if (_activeNotes.Contains(note)) _activeNotes.Remove(note);
    note.Deactivate();
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
       // 1. 在你的 _pool 列表里找
       for (int i = 0; i < _pool.Count; i++) 
       {
           if (_pool[i] != null && !_pool[i].activeInHierarchy) 
           {
               return _pool[i];
           }
       }
       
       // 2. 如果池子里没有可用的，就生成一个新的
       GameObject newNote = Instantiate(notePrefab);
       _pool.Add(newNote); 
       return newNote;
   }
}
