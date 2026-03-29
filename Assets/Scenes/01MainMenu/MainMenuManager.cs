using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // ==========================================
    // 1. UI 面板引用区 (连线双保险)
    // ==========================================
    [Header("UI 面板引用")]
    [Tooltip("主菜单面板 (装着开始、退出按钮的那个)")]
    public GameObject mainMenuPanel; 

    [Tooltip("选关面板 (装着第一关、第二关按钮的那个)")]
    public GameObject levelSelectPanel; 

    // ==========================================
    // 2. 场景跳转配置
    // ==========================================
    [Header("场景跳转配置")]
    public string gameplaySceneName = "GameplayScene";

    // ==========================================
    // 3. 初始化：开局只留“大门”
    // ==========================================
    void Start()
    {
        // 游戏启动：主菜单开，选关面板关
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
    }

    // ==========================================
    // 4. 面板切换逻辑 (跷跷板算法)
    // ==========================================

    /// <summary>
    /// 【开启选关】：隐藏主菜单，显示选关
    /// </summary>
    public void OpenLevelSelect()
    {
        Debug.Log("【UI 切换】进入选关界面");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false); // 隐藏自己
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true); // 开启对方
    }

    /// <summary>
    /// 【返回主菜单】：隐藏选关，显示主菜单
    /// </summary>
    public void CloseLevelSelect()
    {
        Debug.Log("【UI 切换】返回主菜单");
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false); // 隐藏选关
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true); // 重新显示主菜单
    }

    // ==========================================
    // 5. 核心：选关并加载 (逻辑不变)
    // ==========================================
    public void SelectAndLoadLevel(LevelData levelToLoad)
    {
        if (levelToLoad == null) return;

        // 只要是从主菜单主动进的，就得先看教学
        GameDataManager.SkipTutorialTemp = false; 
        GameDataManager.SelectedLevel = levelToLoad;

        if (!string.IsNullOrEmpty(gameplaySceneName))
            SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}