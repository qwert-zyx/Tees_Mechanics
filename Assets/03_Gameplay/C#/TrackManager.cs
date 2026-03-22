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

    void ChangePlayerColor(int newColor)
    {
        playerColorState = newColor;
        judgmentLine.SetBaseColor(newColor);
        Debug.Log($"<color=white>[状态] 切换至模式: {newColor}</color>");
    }

   public void HandleHitInput()
    {
        if (_activeNotes.Count == 0) return;

        // 永远只判定列表中最前面的音符
        NoteController targetNote = _activeNotes[0];
        float diff = Mathf.Abs(GetSongTime() - targetNote.hitTime);

        // 防误触：如果最近的音符还远在 0.3s 之外，不判定
        if (diff > 0.3f) return;

        // 【核心判定流】
        if (targetNote.noteType == playerColorState) // 颜色对了吗？
        {
            if (diff <= perfectThreshold) ExecuteJudgment(JudgmentType.Perfect, targetNote);
            else if (diff <= goodThreshold) ExecuteJudgment(JudgmentType.Good, targetNote);
            else ExecuteJudgment(JudgmentType.Miss, targetNote); // 颜色对但时机太歪
        }
        else
        {
            // 颜色错了，直接 Miss
            ExecuteJudgment(JudgmentType.Miss, targetNote);
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
        // 1. 统计数据
        switch (type)
        {
            case JudgmentType.Perfect: perfectCount++; break;
            case JudgmentType.Good: goodCount++; break;
            case JudgmentType.Miss: missCount++; break;
        }

        // 2. 触发反馈接口
        judgmentLine.ApplyJudgment(type);

        // 3. 回收音符
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

    GameObject GetPooledNote()
    {
        foreach (var n in _pool) if (!n.activeInHierarchy) return n;
        GameObject newNote = Instantiate(notePrefab);
        _pool.Add(newNote);
        return newNote;
    }
}