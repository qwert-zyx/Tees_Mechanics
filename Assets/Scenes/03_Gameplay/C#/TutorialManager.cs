using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; 

public class TutorialManager : MonoBehaviour
{
    [Header("关联组件")]
    public TrackManager trackManager;
    public TextMeshProUGUI tutorialText; 

    // ==========================================
    // 教程文案与参数配置
    // ==========================================
    [Header("教程文案配置")]
    [Tooltip("第一步：引导向左转")]
    public string textTurnLeft = "向左转头，切换红色";
    
    [Tooltip("第二步：引导回正")]
    public string textCenter = "脑袋居中，切换白色";
    
    [Tooltip("第三步：引导向右转")]
    public string textTurnRight = "向右转头，切换蓝色";
    
    [Tooltip("第四步：转头完成后的夸奖")]
    public string textPraiseLook = "完美！准备迎接第一个音符！";
    
    [Tooltip("第五步：音符到达时提示点击")]
    public string textHitNote = "转到对应颜色，点头敲击！";
    
    [Tooltip("终点：教学结束，准备重开的提示")]
    public string textFinishTutorial = "教学完成，正式开始！";

    [Header("节奏控制")]
    [Tooltip("教学结束后停顿几秒再重开。建议 2.0")]
    public float restartDelay = 2.0f;

    // 内部状态控制器
    private int _step = 0; 
    private float _timer = 0f; 
    private int _initialNoteCount = 0; 

    void Start()
    {
        if (GameDataManager.SelectedLevel != null && 
            GameDataManager.SelectedLevel.isTutorial && 
            !GameDataManager.SkipTutorialTemp) 
        {
            _step = 1;
            if (tutorialText != null)
            {
                tutorialText.gameObject.SetActive(true);
                tutorialText.text = "";
            }
        }
        else
        {
            gameObject.SetActive(false); 
        }
    }

    void Update()
    {
        if (_step == 0) return;

        switch (_step)
        {
            case 1: // 开局等待
                _timer += Time.deltaTime;
                if (_timer >= 1.5f) { trackManager.PauseGame(); tutorialText.text = textTurnLeft; _step = 2; }
                break;

            case 2: // 等待左转
                if (trackManager.playerColorState == 0) { trackManager.ResumeGame(); tutorialText.text = ""; _timer = 0f; _step = 3; }
                break;

            case 3: // 等待 1 秒教白色
                _timer += Time.deltaTime;
                if (_timer >= 1f) { trackManager.PauseGame(); tutorialText.text = textCenter; _step = 4; }
                break;

            case 4: // 等待白色
                if (trackManager.playerColorState == 1) { trackManager.ResumeGame(); tutorialText.text = ""; _timer = 0f; _step = 5; }
                break;

            case 5: // 等待 1 秒教蓝色
                _timer += Time.deltaTime;
                if (_timer >= 1f) { trackManager.PauseGame(); tutorialText.text = textTurnRight; _step = 6; }
                break;

            case 6: // 等待蓝色
                if (trackManager.playerColorState == 2) { trackManager.ResumeGame(); tutorialText.text = textPraiseLook; _timer = 0f; _step = 7; }
                break;

            case 7: // 等待音符进场
                _timer += Time.deltaTime;
                if (_timer > 2f) tutorialText.text = ""; 
                if (trackManager.IsNextNoteInHitZone(0.1f)) { trackManager.PauseGame(); tutorialText.text = textHitNote; _initialNoteCount = trackManager.GetActiveNoteCount(); _step = 8; }
                break;

            case 8: // 等待打击
                if (trackManager.GetActiveNoteCount() < _initialNoteCount)
                {
                    // 玩家打击成功的瞬间，虽然 Resume 了，但我们要立刻切到下一步去执行停顿
                    trackManager.ResumeGame();
                    tutorialText.text = textFinishTutorial;
                    _timer = 0f;
                    _step = 9;
                }
                break;

            case 9: // 【停顿重开步】
                // 进入这一步时，再次调用 PauseGame，让音乐和音符在最后一刻“定格”
                trackManager.PauseGame(); 
                
                _timer += Time.deltaTime;
                // 使用面板配置的 restartDelay 变量
                if (_timer >= restartDelay) 
                {
                    GameDataManager.SkipTutorialTemp = true;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    _step = 0;
                }
                break;
        }
    }
}