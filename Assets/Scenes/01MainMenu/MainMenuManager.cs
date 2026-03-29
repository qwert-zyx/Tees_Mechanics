using UnityEngine;
using UnityEngine.SceneManagement; // 记得加上这个，用于切场景

public class MainMenuManager : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject mainMenuPanel;   // 拖入主菜单面板
    public GameObject levelSelectPanel; // 拖入选关面板

    // --- 1. 面板切换逻辑 ---

    // 点击“开始游戏”时调用
    public void OpenLevelSelect()
    {
        mainMenuPanel.SetActive(false);    // 隐藏主菜单
        levelSelectPanel.SetActive(true);  // 显示选关界面
    }

    // 点击“返回”时调用
    public void BackToMainMenu()
    {
        mainMenuPanel.SetActive(true);     // 显示主菜单
        levelSelectPanel.SetActive(false); // 隐藏选关界面
    }

    // --- 2. 关卡跳转逻辑 ---

    // 选关按钮调用，传入关卡名字
    public void LoadGameLevel(string sceneName)
    {
        Debug.Log("正在加载关卡：" + sceneName);
        SceneManager.LoadScene(sceneName); // 直接跳转到对应关卡场景
    }

    // --- 3. 退出逻辑 ---
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}