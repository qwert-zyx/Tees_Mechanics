using UnityEngine;
using System.Collections;

public class HitFeedbackManager : MonoBehaviour
{
    // === [ 核心引用 ] ===
    [Header("关联你的物体")]
    public TrackManager gameTrackManager;
    public Transform judgmentLineTransform; 

    // === [ 震屏设置 (新增) ] ===
    [Header("震屏设置 (空挥/Miss时触发)")]
    public Camera mainCamera;
    public float shakeDuration = 0.1f;    // 震动持续时间 
    public float shakeMagnitude = 0.1f;   // 震动幅度 (可根据手感微调)
    
    // === [ 弹飞特效设置 ] ===
    [Header("音符弹飞设置")]
    public GameObject flyingNotePrefab; 
    public float flyExplosionForce = 10f; 
    [Range(0f, 60f)] public float maxFlyAngleOffset = 30f; 

    // === [ 旧版膨胀和音效设置 ] ===
    [Header("旧版膨胀和音效设置")]
    [Range(1.0f, 1.3f)] public float swellMultiplier = 1.1f; 
    public float swellDuration = 0.05f;
    public float decayDuration = 0.1f;
    public AudioSource hitAudioSource; 
    public AudioClip hitSoundClip;

    private Vector3 _originalScale;
    private Coroutine _swellCoroutine;

    private Vector3 _originalCameraPos;
    private Coroutine _shakeCoroutine;

    void Start()
    {
        if (judgmentLineTransform != null) _originalScale = judgmentLineTransform.localScale;

        // 获取主摄像机初始位置，用于震屏归位
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) _originalCameraPos = mainCamera.transform.localPosition;
    }

    // === [ 接口 1：处理击中与弹飞 ] ===
    public void TriggerHitFeedback(Vector3 hitPosition, Color targetColor)
    {
        TriggerSwell();
        PlayHitSound();
        TriggerNoteFlying(hitPosition, targetColor);
    }

    private void TriggerSwell()
    {
        if (judgmentLineTransform == null) return;
        if (_swellCoroutine != null) StopCoroutine(_swellCoroutine);
        _swellCoroutine = StartCoroutine(SwellProcess());
    }

    private IEnumerator SwellProcess()
    {
        float elapsed = 0f;
        Vector3 targetScale = _originalScale * swellMultiplier;
        while (elapsed < swellDuration)
        {
            judgmentLineTransform.localScale = Vector3.Lerp(_originalScale, targetScale, elapsed / swellDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        judgmentLineTransform.localScale = targetScale;
        elapsed = 0f;
        while (elapsed < decayDuration)
        {
            judgmentLineTransform.localScale = Vector3.Lerp(targetScale, _originalScale, elapsed / decayDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        judgmentLineTransform.localScale = _originalScale;
    }

    private void PlayHitSound()
    {
        if (hitAudioSource != null && hitSoundClip != null)
        {
            hitAudioSource.PlayOneShot(hitSoundClip);
        }
    }

    private void TriggerNoteFlying(Vector3 hitPosition, Color targetColor)
    {
        if (flyingNotePrefab == null) return;

        GameObject flyingNote = Instantiate(flyingNotePrefab, hitPosition, Quaternion.identity);
        
        // 改变特效音符颜色
        SpriteRenderer sr = flyingNote.GetComponent<SpriteRenderer>();
        if (sr != null) 
        {
            sr.color = targetColor;
        }
        
        Rigidbody2D rb = flyingNote.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float randomAngle = Random.Range(-maxFlyAngleOffset, maxFlyAngleOffset);
            Vector2 flyDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
            rb.AddForce(flyDirection * flyExplosionForce, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-50f, 50f));
        }
    }

    // === [ 接口 2：处理失误震屏 ] ===
    public void TriggerMissShake()
    {
        if (mainCamera == null) return;
        
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeProcess());
    }

    private IEnumerator ShakeProcess()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            mainCamera.transform.localPosition = new Vector3(_originalCameraPos.x + x, _originalCameraPos.y + y, _originalCameraPos.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.localPosition = _originalCameraPos;
    }
}