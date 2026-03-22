using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NoteData {
    public float time; // 击中时间（秒）
    public int type;   // 颜色类型
}

public class TrackManager : MonoBehaviour
{
    [Header("参考位点")]
    public Transform spawnPoint;
    public Transform targetPoint;

    [Header("游戏参数")]
    public float noteTravelTime = 2.0f; 
    public AudioSource musicSource;
    public GameObject notePrefab;
    
    [Header("谱面数据")]
    public List<NoteData> sheetMusic = new List<NoteData>();

    private int _nextNoteIndex = 0;
    private List<GameObject> _pool = new List<GameObject>();
    private double _songStartTime; // 歌曲开始时的绝对时间点

    void Start()
    {
        // 【关键】记录音乐开始那一刻的上帝时间
        _songStartTime = AudioSettings.dspTime;
        
        if (musicSource != null)
        {
            musicSource.Play();
            Debug.Log("<color=green>[系统] 音乐开始播放，计时器归零。</color>");
        }
        else 
        {
            Debug.LogError("未关联 AudioSource！");
        }
    }

    // 提供给音符调用的公共方法：获取当前歌曲播放了多少秒
    public float GetSongTime()
    {
        return (float)(AudioSettings.dspTime - _songStartTime);
    }

    void Update()
    {
        // 使用相对时间判断生成
        float songTime = GetSongTime();

        if (_nextNoteIndex < sheetMusic.Count)
        {
            // 如果 歌曲时间 达到了 (音符击中时间 - 飞行时间)
            if (songTime >= sheetMusic[_nextNoteIndex].time - noteTravelTime)
            {
                SpawnNote(sheetMusic[_nextNoteIndex], _nextNoteIndex);
                _nextNoteIndex++;
            }
        }
    }

    void SpawnNote(NoteData data, int index)
    {
        GameObject note = GetPooledNote();
        note.SetActive(true);
        
        Debug.Log($"<color=cyan>[生成] 第 {index} 个音符出发!</color> 目标:{data.time}s, 当前歌曲时间:{GetSongTime():F2}s");

        NoteController nc = note.GetComponent<NoteController>();
        if (nc != null)
        {
            nc.Initialize(index, data.time, noteTravelTime, spawnPoint.position, targetPoint.position, data.type);
        }
    }

    GameObject GetPooledNote()
    {
        foreach (var n in _pool) if (!n.activeInHierarchy) return n;
        GameObject newNote = Instantiate(notePrefab);
        _pool.Add(newNote);
        return newNote;
    }

    void OnDrawGizmos()
    {
        if (spawnPoint == null || targetPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(spawnPoint.position, 0.3f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(targetPoint.position, 0.3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(spawnPoint.position, targetPoint.position);
    }
}