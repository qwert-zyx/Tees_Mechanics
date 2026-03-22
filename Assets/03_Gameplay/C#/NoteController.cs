using UnityEngine;

public class NoteController : MonoBehaviour
{
    public int noteIndex;
    public float hitTime;
    public int noteType;
    private float _travelTime;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private bool _isActive = false;
    private TrackManager _trackManager;

    public void Initialize(int index, float targetTime, float travel, Vector3 start, Vector3 end, int type)
    {
        _trackManager = FindObjectOfType<TrackManager>();
        noteIndex = index;
        hitTime = targetTime;
        _travelTime = travel;
        _startPos = start;
        _endPos = end;
        noteType = type;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (type == 0) sr.color = Color.red;
        else if (type == 1) sr.color = Color.white;
        else sr.color = Color.blue;

        _isActive = true;
    }

    void Update()
    {
        if (!_isActive || _trackManager == null) return;

        float songTime = _trackManager.GetSongTime();
        float progress = (songTime - (hitTime - _travelTime)) / _travelTime;
        transform.position = Vector3.Lerp(_startPos, _endPos, progress);

        // 漏检判定：超过判定时间 0.15 秒自动 Miss
        if (songTime > hitTime + 0.15f)
        {
            _trackManager.HandlePassiveMiss(this);
        }
    }

    public void Deactivate()
    {
        _isActive = false;
        gameObject.SetActive(false);
    }
}