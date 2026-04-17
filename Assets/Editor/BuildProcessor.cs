using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEngine;

public class BuildProcessor
{
    // 这个函数会在打包完成后自动执行
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        // 1. 定义源路径（你插件里放模型的地方，根据你实际路径改一下）
        string sourcePath = Path.Combine(Application.dataPath, "Mediapipe/Models"); 
        
        // 2. 定义目标路径（打包后的 _Data 文件夹）
        string rootPath = Path.GetDirectoryName(pathToBuiltProject);
        string folderName = Path.GetFileNameWithoutExtension(pathToBuiltProject) + "_Data";
        string targetPath = Path.Combine(rootPath, folderName, "StreamingAssets/Mediapipe");

        // 3. 自动执行复制
        if (Directory.Exists(sourcePath))
        {
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            
            foreach (var file in Directory.GetFiles(sourcePath, "*.tflite"))
            {
                string destFile = Path.Combine(targetPath, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            Debug.Log("<color=green>【自动化】已自动将 MediaPipe 模型搬运至打包目录！</color>");
        }
    }
}