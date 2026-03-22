using UnityEngine;

public class NoteController : MonoBehaviour
{
    [Header("调试信息")]
    public int noteIndex;      
    public float hitTime;      
    
    private float _travelTime;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private bool _isActive = false;
    private bool _hasReachedLogSent = false; 

    private TrackManager _trackManager;

    void Awake()
    {
        // 提前找好引用，避免在 Update 里频繁寻找
        _trackManager = FindObjectOfType<TrackManager>();
    }

    public void Initialize(int index, float targetTime, float travel, Vector3 start, Vector3 end, int type)
    {
        noteIndex = index;
        hitTime = targetTime;
        _travelTime = travel;
        _startPos = start;
        _endPos = end;
        _hasReachedLogSent = false;
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (type == 0) sr.color = Color.red;
            else if (type == 1) sr.color = Color.green;
            else sr.color = Color.blue;
        }

        _isActive = true;
    }

    void Update()
    {
        if (!_isActive || _trackManager == null) return;

        // 【关键】使用相对时间（歌曲进度）来计算
        float songTime = _trackManager.GetSongTime();
        
        float startTime = hitTime - _travelTime;
        float progress = (songTime - startTime) / _travelTime;

        // 时间驱动的线性插值移动
        transform.position = Vector3.Lerp(_startPos, _endPos, progress);

        // Debug: 到达判定线的一瞬间
        if (progress >= 1.0f && !_hasReachedLogSent)
        {
            Debug.Log($"<color=yellow>[判定] 第 {noteIndex} 个音符到达! </color> 歌曲时间:{songTime:F2}s, 预期:{hitTime}s");
            _hasReachedLogSent = true; 
        }

        // Miss 自动回收 (飞过判定线 10% 的距离)
        if (progress > 1.1f)
        {
            JudgmentLineController jlc = FindObjectOfType<JudgmentLineController>();
            if (jlc != null) jlc.TriggerFeedback(false);
            
            _isActive = false;
            gameObject.SetActive(false);
        }
    }
}