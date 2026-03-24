using UnityEngine;
using System.Collections;

public class HitFeedbackManager : MonoBehaviour
{
    // === [ 核心引用 ] ===
    [Header("关联你的物体 (拖拽过来)")]
    // 这里拖入你的 TrackManager (它里面有打击逻辑)
    public TrackManager gameTrackManager;
    // 这里拖入你要膨胀的判定线 Transform
    public Transform judgmentLineTransform; 
    
    // === [ 膨胀反馈设置 ] ===
    [Header("膨胀设置")]
    // 膨胀到原来的多少倍? (建议 1.05 到 1.15)
    [Range(1.0f, 1.3f)] public float swellMultiplier = 1.1f; 
    // 膨胀过程持续多久? (越短越有爆发力，建议 0.05)
    public float swellDuration = 0.05f;
    // 渐变回原状持续多久? (建议 0.1)
    public float decayDuration = 0.1f;

    // === [ 听觉反馈设置 ] ===
    [Header("音效设置")]
    // 这里拖入你的 AudioSource 组件 (可以挂在自身或 TrackManager 上)
    public AudioSource hitAudioSource; 
    // 这里拖入你的打击音效 (.wav 或 .mp3)
    public AudioClip hitSoundClip;

    private Vector3 _originalScale; // 记录原比例
    private Coroutine _swellCoroutine; // 用于控制协同程序，防止重叠

    void Start()
    {
        // 游戏启动时，记录判定线的原比例
        if (judgmentLineTransform != null)
        {
            _originalScale = judgmentLineTransform.localScale;
        }
        else
        {
            Debug.LogError("[反馈] 忘记把判定线 Transform 拖进来了！");
        }
    }

    // === [ 【重点】这是对外暴露的打击接口 ] ===
    // 玩家点头时，我们在 HeadPoseAdapter 里只改一小行代码来调用它
    public void TriggerHitFeedback()
    {
        // 1. 物理膨胀 (弹开感)
        TriggerSwell();
        // 2. 听觉打击 (清脆感)
        PlayHitSound();
    }

    // 内部函数：处理膨胀逻辑
    private void TriggerSwell()
    {
        if (judgmentLineTransform == null) return;

        // 如果上一次膨胀还没完，直接把它掐断，重头开始
        if (_swellCoroutine != null) StopCoroutine(_swellCoroutine);
        
        // 开始新的膨胀协同程序
        _swellCoroutine = StartCoroutine(SwellProcess());
    }

    // 协同程序：处理Scale的渐变
    private IEnumerator SwellProcess()
    {
        // 第一阶段：快速膨胀 (Scale Up)
        float elapsed = 0f;
        Vector3 targetScale = _originalScale * swellMultiplier;

        while (elapsed < swellDuration)
        {
            // 用 Lerp (线性插值) 制造平滑的膨胀效果
            judgmentLineTransform.localScale = Vector3.Lerp(_originalScale, targetScale, elapsed / swellDuration);
            elapsed += Time.deltaTime;
            yield return null; // 等待下一帧
        }
        judgmentLineTransform.localScale = targetScale; // 确保完全达到目标 Scale

        // 第二阶段：渐变回原状 (Scale Down)
        elapsed = 0f;
        while (elapsed < decayDuration)
        {
            judgmentLineTransform.localScale = Vector3.Lerp(targetScale, _originalScale, elapsed / decayDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        judgmentLineTransform.localScale = _originalScale; // 确保完全回原 Scale
    }

    // 内部函数：播放音效
    private void PlayHitSound()
    {
        if (hitAudioSource != null && hitSoundClip != null)
        {
            // 使用 PlayOneShot 可以防止音效叠加时的噪音，且效率很高
            hitAudioSource.PlayOneShot(hitSoundClip);
        }
    }
}