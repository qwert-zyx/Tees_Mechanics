using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("关联组件")]
    public TrackManager trackManager;
    public TextMeshProUGUI tutorialText; // 屏幕中间显示提示的大字

    // ==========================================
    // 【新增】：教程文案配置区 (支持多语言无缝切换)
    // ==========================================
    [Header("教程文案配置 (直接在这里改成英文即可)")]
    
    [Tooltip("第一步：引导玩家向左转头时的提示。英文参考: Turn left to switch to RED")]
    public string textTurnLeft = "向左转头，切换红色";
    
    [Tooltip("第二步：引导玩家回正脑袋时的提示。英文参考: Center your head for WHITE")]
    public string textCenter = "脑袋居中，切换白色";
    
    [Tooltip("第三步：引导玩家向右转头时的提示。英文参考: Turn right to switch to BLUE")]
    public string textTurnRight = "向右转头，切换蓝色";
    
    [Tooltip("第四步：完成三种颜色切换教学后的夸奖语。英文参考: Perfect! Get ready for the first note!")]
    public string textPraiseLook = "完美！准备迎接第一个音符！";
    
    [Tooltip("第五步：当第一个音符到达判定线时，提示玩家点头敲击。英文参考: Match the color and nod to hit!")]
    public string textHitNote = "转到对应颜色，点头敲击！";
    
    [Tooltip("终点：玩家成功打掉（或漏掉）第一个音符后的结束语。英文参考: Nice! The game officially begins!")]
    public string textFinishTutorial = "漂亮！游戏正式开始！";

    // 内部状态控制器
    private int _step = 0; // 当前教学进度
    private float _timer = 0f; // 计时器
    private int _initialNoteCount = 0; // 记录屏幕上有几个音符

    void Start()
    {
        // 1. 判断当前关卡的“档案卡”里，有没有勾选“是新手教程”？
        if (GameDataManager.SelectedLevel != null && GameDataManager.SelectedLevel.isTutorial)
        {
            _step = 1; // 激活教练！
            if (tutorialText != null)
            {
                tutorialText.gameObject.SetActive(true);
                tutorialText.text = "";
            }
        }
        else
        {
            // 如果不是教程关，把自己连同 UI 一起关掉，绝不干扰游戏！
            gameObject.SetActive(false); 
        }
    }

    void Update()
    {
        if (_step == 0) return; // 0 表示没事干

        switch (_step)
        {
            case 1: // 第一步：开局等 1.5 秒
                _timer += Time.deltaTime;
                if (_timer >= 1.5f)
                {
                    trackManager.PauseGame(); // 砸瓦鲁多！时间停止！
                    tutorialText.text = textTurnLeft; // 读取面板配置的文案
                    _step = 2;
                }
                break;

            case 2: // 第二步：死等玩家切成红色 (状态 0)
                if (trackManager.playerColorState == 0)
                {
                    trackManager.ResumeGame(); // 时间流动
                    tutorialText.text = "";
                    _timer = 0f;
                    _step = 3;
                }
                break;

            case 3: // 第三步：过 1 秒后，教白色
                _timer += Time.deltaTime;
                if (_timer >= 1f)
                {
                    trackManager.PauseGame();
                    tutorialText.text = textCenter; // 读取面板配置的文案
                    _step = 4;
                }
                break;

            case 4: // 第四步：死等白色 (状态 1)
                if (trackManager.playerColorState == 1)
                {
                    trackManager.ResumeGame();
                    tutorialText.text = "";
                    _timer = 0f;
                    _step = 5;
                }
                break;

            case 5: // 第五步：过 1 秒后，教蓝色
                _timer += Time.deltaTime;
                if (_timer >= 1f)
                {
                    trackManager.PauseGame();
                    tutorialText.text = textTurnRight; // 读取面板配置的文案
                    _step = 6;
                }
                break;

            case 6: // 第六步：死等蓝色 (状态 2)
                if (trackManager.playerColorState == 2)
                {
                    trackManager.ResumeGame();
                    tutorialText.text = textPraiseLook; // 读取面板配置的文案
                    _timer = 0f;
                    _step = 7;
                }
                break;

            case 7: // 第七步：等待第一个音符到达判定线
                _timer += Time.deltaTime;
                if (_timer > 2f) tutorialText.text = ""; // 隐藏上面的夸奖文字

                // 用雷达探测：是否有音符进入了打击区？
                if (trackManager.IsNextNoteInHitZone(0.1f))
                {
                    trackManager.PauseGame(); // 再次时停
                    tutorialText.text = textHitNote; // 读取面板配置的文案
                    _initialNoteCount = trackManager.GetActiveNoteCount(); // 记录当前音符数
                    _step = 8;
                }
                break;

            case 8: // 第八步：等待玩家点头打击
                // 如果屏幕上的音符变少了（说明玩家成功击碎或漏接了）
                if (trackManager.GetActiveNoteCount() < _initialNoteCount)
                {
                    trackManager.ResumeGame();
                    tutorialText.text = textFinishTutorial; // 读取面板配置的文案
                    _timer = 0f;
                    _step = 9;
                }
                break;

            case 9: // 最终步：淡出字幕，深藏功与名
                _timer += Time.deltaTime;
                if (_timer > 1.5f)
                {
                    tutorialText.gameObject.SetActive(false);
                    _step = 0; // 教练下班！
                }
                break;
        }
    }
}