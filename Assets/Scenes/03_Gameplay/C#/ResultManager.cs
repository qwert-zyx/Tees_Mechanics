using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // 必须引入这个来使用 List

// ==========================================
// 【新增】：自定义评语规则类
// 加上 Serializable 标签，它就能在 Unity 的 Inspector 里显示出来了！
// ==========================================
[System.Serializable]
public class CommentRule
{
    [Tooltip("当漏接数量 大于等于 这个值时，触发此条评语")]
    public int minMisses; 
    
    [Tooltip("评语文案，文案中写 {0} 的地方会自动被替换成实际漏接数字")]
    public string commentTemplate;
}

public class ResultManager : MonoBehaviour
{
    // ==========================================
    // 1. UI 组件引用区
    // ==========================================
    [Header("面板引用")]
    public GameObject resultPanel; 

    [Header("文本引用")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI maxComboText;
    public TextMeshProUGUI missCountText;
    public TextMeshProUGUI commentText;

    [Header("按钮引用")]
    public Button nextLevelButton; 
    public TextMeshProUGUI nextLevelButtonText; 

    // ==========================================
    // 2. 文案配置区 (支持无限扩展！)
    // ==========================================
    [Header("结算数据文案前缀")]
    public string scorePrefix = "最终得分: ";       
    public string comboPrefix = "最高连击: ";       
    public string missPrefix = "漏接数量: ";        

    [Header("评语规则配置表 (请在面板里从大到小排列)")]
    // 我为你预设了四个等级，你可以随时在 Unity 面板里增加或修改
    public List<CommentRule> commentRules = new List<CommentRule>()
    {
        new CommentRule { minMisses = 11, commentTemplate = "漏了 {0} 个！后宫佳丽三千，海王非你莫属！" },
        new CommentRule { minMisses = 4,  commentTemplate = "漏了 {0} 个！常客！你这属于办了年卡的。" },
        new CommentRule { minMisses = 1,  commentTemplate = "漏了 {0} 个！逢场作戏，仅有几面之缘的小姐。" },
        new CommentRule { minMisses = 0,  commentTemplate = "完美！{0} 漏接，洁身自好！" }
    };

    [Header("动态按钮文案")]
    public string btnTextNextLevel = "下一关";      
    public string btnTextFinishGame = "完成游戏";   

    private LevelData _nextLevelData; 

    void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    // ==========================================
    // 核心接口：接收数据并展示
    // ==========================================
    public void ShowResult(int score, int maxCombo, int missCount, LevelData nextLevel)
    {
        _nextLevelData = nextLevel;
        
        if (resultPanel != null) resultPanel.SetActive(true);

        if (finalScoreText != null) finalScoreText.text = $"{scorePrefix}{score:N0}";
        if (maxComboText != null) maxComboText.text = $"{comboPrefix}{maxCombo}";
        if (missCountText != null) missCountText.text = $"{missPrefix}{missCount}";

        // ==========================================
        // 【核心改动】：动态匹配评语与数量填坑
        // ==========================================
        if (commentText != null)
        {
            // 先按要求的数量从大到小排序，确保高难度的评语优先触发
            commentRules.Sort((a, b) => b.minMisses.CompareTo(a.minMisses));

            string finalComment = "";
            // 遍历我们配置的规则表
            foreach (var rule in commentRules)
            {
                // 只要实际漏接数“大于等于”规则要求的值，就采用这条评语
                if (missCount >= rule.minMisses)
                {
                    // string.Format 会自动把 rule.commentTemplate 里的 {0} 替换成 missCount
                    finalComment = string.Format(rule.commentTemplate, missCount);
                    break; // 找到了就立刻停下
                }
            }
            commentText.text = finalComment;
        }

        // 处理下一关按钮状态
        if (nextLevelButton != null && nextLevelButtonText != null)
        {
            nextLevelButton.gameObject.SetActive(true); 
            if (_nextLevelData != null) nextLevelButtonText.text = btnTextNextLevel; 
            else nextLevelButtonText.text = btnTextFinishGame; 
        }
    }

    // ==========================================
    // 按钮绑定的功能区
    // ==========================================
    public void Btn_RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void Btn_ReturnToMenu() 
    {
        SceneManager.LoadScene("MainMenu"); 
    }

    public void Btn_LoadNextLevel()
    {
        if (_nextLevelData != null)
        {
            GameDataManager.SelectedLevel = _nextLevelData;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else SceneManager.LoadScene("MainMenu");
    }
}