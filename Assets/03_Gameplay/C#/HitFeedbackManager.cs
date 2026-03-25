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
    // 这里拖入你刚才做的 VFX_FlyingNote 预制体
    public GameObject flyingNotePrefab; 
    // 弹飞的初始爆发力有多大? (建议 5 到 15)
    public float flyExplosionForce = 10f; 
    // 弹飞时向左或向右偏移的最大角度 (建议 20 到 45)
    [Range(0f, 60f)] public float maxFlyAngleOffset = 30f; 

    // === [ 其他设置 (保留之前的) ] ===
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

    // === [ 重点：这是对外的打击接口 ] ===
    // 我们需要修改它，接收一个“当前音符坐标”的参数
    public void TriggerHitFeedback(Vector3 hitPosition)
    {
        // 1. 物理膨胀 (弹开感)
        TriggerSwell();
        // 2. 听觉打击 (清脆感)
        PlayHitSound();
        // 3. 【核心新增】音符弹飞 (冲击感)
        TriggerNoteFlying(hitPosition);
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

    // === [ 【核心新增】弹飞逻辑 ] ===
    private void TriggerNoteFlying(Vector3 hitPosition)
    {
        if (flyingNotePrefab == null) return;

        // 1. 在打击点生成替身
        GameObject flyingNote = Instantiate(flyingNotePrefab, hitPosition, Quaternion.identity);
        
        // 2. 获取替身的刚体
        Rigidbody2D rb = flyingNote.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 3. 计算弹飞方向：
            // 默认向上 (Vector2.up)
            // 加上一个随机的水平偏移 (-maxFlyAngleOffset 到 +maxFlyAngleOffset)
            float randomAngle = Random.Range(-maxFlyAngleOffset, maxFlyAngleOffset);
            Vector2 flyDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;

            // 4. 施加瞬间爆发力 (ForceMode2D.Impulse 是爆炸效果的核心)
            rb.AddForce(flyDirection * flyExplosionForce, ForceMode2D.Impulse);
            
            // 可选：给它加一点随机的旋转力，让它飞的时候转起来
            rb.AddTorque(Random.Range(-50f, 50f));
        }
    }
}