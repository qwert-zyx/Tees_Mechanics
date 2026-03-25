using UnityEngine;
using System.Collections;

public class HitFeedbackManager : MonoBehaviour
{
    // === [ 核心引用 ] ===
    [Header("关联你的物体")]
    public TrackManager gameTrackManager;
    public Transform judgmentLineTransform; 
    
    // === [ 弹飞特效设置 ] ===
    [Header("音符弹飞设置")]
    public GameObject flyingNotePrefab; 
    public float flyExplosionForce = 10f; 
    [Range(0f, 60f)] public float maxFlyAngleOffset = 30f; 

    // === [ 其他设置 ] ===
    [Header("旧版膨胀和音效设置")]
    [Range(1.0f, 1.3f)] public float swellMultiplier = 1.1f; 
    public float swellDuration = 0.05f;
    public float decayDuration = 0.1f;
    public AudioSource hitAudioSource; 
    public AudioClip hitSoundClip;

    private Vector3 _originalScale;
    private Coroutine _swellCoroutine;

    void Start()
    {
        if (judgmentLineTransform != null) _originalScale = judgmentLineTransform.localScale;
    }

    // === [ 重点修改：增加了 Color targetColor 参数 ] ===
    public void TriggerHitFeedback(Vector3 hitPosition, Color targetColor)
    {
        TriggerSwell();
        PlayHitSound();
        // 传递颜色给弹飞逻辑
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
        if (hitAudioSource != null && hitSoundClip != null) hitAudioSource.PlayOneShot(hitSoundClip);
    }

    // === [ 重点修改：改变替身颜色 ] ===
    private void TriggerNoteFlying(Vector3 hitPosition, Color targetColor)
    {
        if (flyingNotePrefab == null) return;

        GameObject flyingNote = Instantiate(flyingNotePrefab, hitPosition, Quaternion.identity);
        
        // 【核心新增】改变音符的颜色！
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
}