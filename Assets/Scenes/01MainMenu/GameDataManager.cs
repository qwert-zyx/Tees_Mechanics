public static class GameDataManager
{
    public static LevelData SelectedLevel;
    
    // 【核心新增】：单局内是否跳过教程的临时标记
    // 默认是 false，只有教程跑完那一刻我们会把它变成 true
    public static bool SkipTutorialTemp = false; 
}