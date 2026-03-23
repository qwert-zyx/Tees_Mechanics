using UnityEngine;
using System.Reflection;
using Mediapipe.Unity;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class HeadPoseAdapter : MonoBehaviour
    {
        // 核心引用：拖入你的 TrackManager
        [Header("关联你的游戏脚本")]
        public TrackManager gameTrackManager; 

        [Header("灵敏度校准")]
        [Range(0.05f, 0.5f)] public float yawThreshold = 0.12f;      
        [Range(0.005f, 0.05f)] public float pitchSensitivity = 0.015f; 

        private float _lastNoseY = 0f;
        private int _lastColorState = 1; // 0:红, 1:白, 2:蓝

        void Update()
        {
            var controller = FindObjectOfType<FaceLandmarkerResultAnnotationController>();
            if (controller == null || gameTrackManager == null) return;

            var resultField = controller.GetType().GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            if (resultField == null) return;

            var resultObj = resultField.GetValue(controller);
            if (resultObj == null) return;

            var result = (Mediapipe.Tasks.Vision.FaceLandmarker.FaceLandmarkerResult)resultObj;
            if (result.faceLandmarks == null || result.faceLandmarks.Count == 0) return;

            var landmarks = result.faceLandmarks[0].landmarks;
            var nose = landmarks[0];
            var leftEye = landmarks[33];
            var rightEye = landmarks[263];

            // 1. 处理转头 -> 切换颜色
            ProcessYaw(nose, leftEye, rightEye);
            // 2. 处理点头 -> 模拟打击
            ProcessPitch(nose);
        }

        void ProcessYaw(Mediapipe.Tasks.Components.Containers.NormalizedLandmark nose, 
                        Mediapipe.Tasks.Components.Containers.NormalizedLandmark left, 
                        Mediapipe.Tasks.Components.Containers.NormalizedLandmark right)
        {
            float offset = (nose.x - (left.x + right.x) / 2f) / Mathf.Abs(right.x - left.x);
            
            int targetState = 1; // 默认白
            if (offset < -yawThreshold) targetState = 0; // 左-红
            else if (offset > yawThreshold) targetState = 2; // 右-蓝

            if (targetState != _lastColorState)
            {
                gameTrackManager.ChangePlayerColor(targetState);
                _lastColorState = targetState;
            }
        }

        void ProcessPitch(Mediapipe.Tasks.Components.Containers.NormalizedLandmark nose)
        {
            float speed = nose.y - _lastNoseY;
            if (speed > pitchSensitivity)
            {
                // 【核心连接】：这里就是触发判定的瞬间！
                gameTrackManager.HandleHitInput();
            }
            _lastNoseY = nose.y;
        }
    }
}