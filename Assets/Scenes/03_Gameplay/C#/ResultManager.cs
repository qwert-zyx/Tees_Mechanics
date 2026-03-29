using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic; 

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
    // 2. 场景跳转配置 (新增：方便你改名字)
    // ==========================================
    [Header("场景跳转配置")]
    
    [Tooltip("主菜单场景的文件名。请确保这个名字和 Build Settings 里的场景名完全一致！")]
    public string mainMenuSceneName = "MainMenu";

    // ==========================================
    // 3. 文案配置区
    // ==========================================
    [Header("结算数据文案前缀")]
    public string scorePrefix = "最终得分: ";       
    public string comboPrefix = "最高连击: ";       
    public string missPrefix = "漏接数量: ";        

    [Header("评语规则配置表 (英文版 Meme)")]
    public List<CommentRule> commentRules = new List<CommentRule>()
    {
        new CommentRule { minMisses = 11, commentTemplate = "You got {0} misses! Certified Lover Boy. You're building a whole harem out here!" },
        new CommentRule { minMisses = 4,  commentTemplate = "You got {0} misses! Bro is a VIP regular. Do you have a monthly subscription?" },
        new CommentRule { minMisses = 1,  commentTemplate = "You got {0} misses! Just a casual flirt. We all make mistakes in the heat of passion." },
        new CommentRule { minMisses = 0,  commentTemplate = "Perfect! {0} misses. Pure, loyal, and absolutely BASED." }
    };

    [Header("动态按钮文案")]
    public string btnTextNextLevel = "下一关";      
    public string btnTextFinishGame = "完成游戏";   

    private LevelData _nextLevelData; 

    void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    public void ShowResult(int score, int maxCombo, int missCount, LevelData nextLevel)
    {
        _nextLevelData = nextLevel;
        if (resultPanel != null) resultPanel.SetActive(true);

        if (finalScoreText != null) finalScoreText.text = $"{scorePrefix}{score:N0}";
        if (maxComboText != null) maxComboText.text = $"{comboPrefix}{maxCombo}";
        if (missCountText != null) missPrefixTextUpdate(missCount);

        // 评语逻辑
        if (commentText != null)
        {
            commentRules.Sort((a, b) => b.minMisses.CompareTo(a.minMisses));
            string finalComment = "";
            foreach (var rule in commentRules)
            {
                if (missCount >= rule.minMisses)
                {
                    finalComment = string.Format(rule.commentTemplate, missCount);
                    break;
                }
            }
            commentText.text = finalComment;
        }

        // 按钮文字处理
        if (nextLevelButton != null && nextLevelButtonText != null)
        {
            nextLevelButton.gameObject.SetActive(true); 
            if (_nextLevelData != null) nextLevelButtonText.text = btnTextNextLevel; 
            else nextLevelButtonText.text = btnTextFinishGame; 
        }
    }

    // 内部辅助显示
    private void missPrefixTextUpdate(int count)
    {
        if (missCountText != null) missCountText.text = $"{missPrefix}{count}";
    }

    // ==========================================
    // 按钮功能区 (现在全部使用配置的场景名)
    // ==========================================
    
    public void Btn_RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void Btn_ReturnToMenu() 
    {
        // 使用面板里配置的名字进行跳转
        SceneManager.LoadScene(mainMenuSceneName); 
    }

    public void Btn_LoadNextLevel()
    {
        if (_nextLevelData != null)
        {
            GameDataManager.SelectedLevel = _nextLevelData;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else 
        {
            // 如果没有下一关了，也跳回主菜单
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}