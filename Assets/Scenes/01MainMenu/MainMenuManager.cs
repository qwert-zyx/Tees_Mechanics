using UnityEngine;
using UnityEngine.SceneManagement; // 必须有这个才能切场景

public class MainMenuManager : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject mainMenuPanel;    // 拖入主菜单面板
    public GameObject levelSelectPanel; // 拖入选关面板

    // --- 1. 面板切换逻辑 ---

    // 点击“开始游戏”时调用
    public void OpenLevelSelect()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);    
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);  
    }

    // 点击“返回”时调用
    public void BackToMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);     
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false); 
    }

    // --- 2. 关卡跳转逻辑（数据驱动高级版） ---

    // 选关按钮调用，接收一张关卡档案卡
    public void SelectAndLoadLevel(LevelData levelToLoad)
    {
        // 1. 把选中的卡片存进全局“存包柜”
        GameDataManager.SelectedLevel = levelToLoad;
        
        // 2. 跳转到唯一的游戏场景 (确保名字和你的场景名一模一样)
        SceneManager.LoadScene("03_Gameplay"); 
    }

    // --- 3. 退出逻辑 ---
    public void QuitGame()
    {
        Debug.Log("玩家点击了退出按钮！");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}