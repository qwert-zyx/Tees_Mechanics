using UnityEngine;

public class JudgmentLineController : MonoBehaviour
{
    [Header("渲染引用")]
    [Tooltip("如果不拖拽，脚本会自动尝试获取自身的 SpriteRenderer")]
    public SpriteRenderer spriteRenderer;

    [Header("颜色设置")]
    public Color defaultColor = Color.black;
    public Color successColor = Color.green;
    public Color failureColor = Color.red;

    [Header("回弹设置")]
    [Tooltip("数值越大，颜色变回黑色的速度越快")]
    public float returnSpeed = 8.0f;

    void Awake()
    {
        // 自动兜底：如果你在 Inspector 里忘了拖组件，代码自己去找
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 初始设为黑色
        spriteRenderer.color = defaultColor;
    }

    void Update()
    {
        // 核心：每帧向黑色插值（回弹逻辑）
        if (spriteRenderer.color != defaultColor)
        {
            spriteRenderer.color = Color.Lerp(
                spriteRenderer.color, 
                defaultColor, 
                Time.deltaTime * returnSpeed
            );
        }

        // 注意：这里我删除了 HandleManualInput，
        // 因为在接下来的“下落判定”逻辑中，
        // 变色应该由“判定结果”触发，而不是由按键直接触发。
    }

    /// <summary>
    /// 核心接口：外部逻辑（比如音符脚本）判定成功或失败后，调用此函数
    /// </summary>
    public void TriggerFeedback(bool isSuccess)
    {
        spriteRenderer.color = isSuccess ? successColor : failureColor;
    }
}