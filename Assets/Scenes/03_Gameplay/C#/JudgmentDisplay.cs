using UnityEngine;
using TMPro;
using System.Collections;

public class JudgmentDisplay : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;

    [Header("动画参数")]
    public float startScale = 0.3f;      // 初始极小
    public float targetScale = 1.0f;     // 弹出后的正常大小
    public float popDuration = 0.08f;    // 弹出速度（越快越有打击感）
    
    [Header("消失参数")]
    public float fadeDelay = 0.4f;       // 维持多久开始变淡
    public float fadeDuration = 0.15f;   // 消失过程

    [Header("漂移参数")]
    public float upwardForce = 50f;      // 缓慢向上漂移的速度

    public void Init(string text, Color color, Vector2 randomOffset)
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
        
        // 1. 设置内容和颜色
        _textMesh.text = text;
        _textMesh.color = color;

        // 2. 设置初始位置（在中心点基础上加一点随机偏移）
        transform.localPosition += (Vector3)randomOffset;

        // 3. 开启自毁动画
        StartCoroutine(AnimateSequence());
    }

    private IEnumerator AnimateSequence()
    {
        // --- 阶段 A: 从小变大 (Pop) ---
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            float lerp = elapsed / popDuration;
            // 简单的缩放插值
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, lerp);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one * targetScale;

        // --- 阶段 B: 维持并微弱漂移 ---
        float timer = 0f;
        while (timer < fadeDelay)
        {
            // 往上慢慢飘一点点
            transform.localPosition += Vector3.up * upwardForce * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        // --- 阶段 C: 变淡并彻底消失 ---
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            _textMesh.color = new Color(_textMesh.color.r, _textMesh.color.g, _textMesh.color.b, alpha);
            
            // 消失时也继续往上飘一点
            transform.localPosition += Vector3.up * upwardForce * Time.deltaTime;
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- 阶段 D: 任务完成，自毁 ---
        Destroy(gameObject);
    }
}