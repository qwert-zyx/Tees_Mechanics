using UnityEngine;

public class JudgmentLineController : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    
    [Header("各状态底色")]
    public Color colorRed = Color.red;
    public Color colorWhite = Color.white;
    public Color colorBlue = Color.blue;

    private Color _currentBaseColor;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetBaseColor(int type)
    {
        if (type == 0) _currentBaseColor = colorRed;
        else if (type == 1) _currentBaseColor = colorWhite;
        else _currentBaseColor = colorBlue;
        spriteRenderer.color = _currentBaseColor;
    }

    void Update()
    {
        // 反馈产生的瞬间亮色平滑回归底色
        if (spriteRenderer.color != _currentBaseColor)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, _currentBaseColor, Time.deltaTime * 12f);
        }
    }

    // ==========================================
    // 【统一反馈处理入口】
    // ==========================================
    public void ApplyJudgment(JudgmentType type)
    {
        switch (type)
        {
            case JudgmentType.Perfect:
                Debug.Log("<color=cyan>[判定反馈] PERFECT! </color>");
                PerformPerfectEffect();
                break;

            case JudgmentType.Good:
                Debug.Log("<color=green>[判定反馈] GOOD </color>");
                PerformGoodEffect();
                break;

            case JudgmentType.Miss:
                Debug.Log("<color=red>[判定反馈] MISS... </color>");
                PerformMissEffect();
                break;
        }
    }

    // --- 以下是三个独立的表现分支，你可以在这里随心所欲加特效 ---

    private void PerformPerfectEffect()
    {
        // 视觉：非常明显的闪烁
        spriteRenderer.color = _currentBaseColor * 3.5f; 
        // TODO: 播放 Perfect 音效、生成粒子效果
    }

    private void PerformGoodEffect()
    {
        // 视觉：中等闪烁
        spriteRenderer.color = _currentBaseColor * 2.0f;
        // TODO: 播放普通音效
    }

    private void PerformMissEffect()
    {
        // 视觉：不闪烁（以免干扰判断），或者可以让线微微抖动
        // TODO: 触发震动接口 (HandTracker.Vibrate)
        // TODO: 播放 Miss 音效
    }
}