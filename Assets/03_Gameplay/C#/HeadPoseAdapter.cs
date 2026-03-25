using UnityEngine;
using System.Reflection;
using Mediapipe.Unity;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class HeadPoseAdapter : MonoBehaviour
    {
        [Header("关联你的游戏脚本 (别忘了拖拽!)")]
        public TrackManager gameTrackManager; 

        [Header("灵敏度校准")]
        [Range(0.05f, 0.5f)] public float yawThreshold = 0.12f;      
        [Range(0.005f, 0.05f)] public float pitchSensitivity = 0.015f; 

        private float _lastNoseY = 0f;
        private int _lastColorState = 1; // 0:红, 1:白, 2:蓝
    // 【新增】打击冷却控制
        [Header("打击间隔控制")]
        public float hitCooldown = 0.2f; // 0.2秒内只允许触发一次
        private float _nextHitTime = 0f;  // 记录下一次允许打击的时间

        void Update()
        {
            // 1. 找数据源和接收器
            var controller = FindObjectOfType<FaceLandmarkerResultAnnotationController>();
            
            // 如果没找到 Controller，或者你忘了把 TrackManager 拖进 Inspector，安全退出
            if (controller == null || gameTrackManager == null) return;

            // 2. 反射获取私有数据包
            var resultField = controller.GetType().GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            if (resultField == null) return;

            var resultObj = resultField.GetValue(controller);
            if (resultObj == null) return;

            var result = (Mediapipe.Tasks.Vision.FaceLandmarker.FaceLandmarkerResult)resultObj;
            
            // ==========================================
            // 【核心防崩溃安全锁】：防止越界报错 (IndexOutOfRange)
            // ==========================================
            // 如果当前画面里没检测到脸，或者数据还在加载中，立刻打住，等下一帧
            if (result.faceLandmarks == null || result.faceLandmarks.Count == 0) return;

            var landmarks = result.faceLandmarks[0].landmarks;
            
            // 二次保险：如果检测到了脸，但 468 个特征点还没完全算出来，也打住
            if (landmarks == null || landmarks.Count < 468) return;

            // 3. 提取关键特征点 (0:鼻尖, 33:左眼, 263:右眼)
            var nose = landmarks[0];
            var leftEye = landmarks[33];
            var rightEye = landmarks[263];

            // 4. 将面部动作转化为音游输入
            ProcessYaw(nose, leftEye, rightEye);
            ProcessPitch(nose);
        }

        void ProcessYaw(Mediapipe.Tasks.Components.Containers.NormalizedLandmark nose, 
                        Mediapipe.Tasks.Components.Containers.NormalizedLandmark left, 
                        Mediapipe.Tasks.Components.Containers.NormalizedLandmark right)
        {
            // 计算鼻尖偏离双眼中心的比例
            float offset = (nose.x - (left.x + right.x) / 2f) / Mathf.Abs(right.x - left.x);
            
            int targetState = 1; // 默认：中（白）
            if (offset < -yawThreshold) targetState = 0;      // 左转：红
            else if (offset > yawThreshold) targetState = 2;  // 右转：蓝

            // 只有当状态发生改变时，才通知 TrackManager 变色，避免每帧重复调用
            if (targetState != _lastColorState)
            {
                gameTrackManager.ChangePlayerColor(targetState);
                _lastColorState = targetState;
            }
        }

        void ProcessPitch(Mediapipe.Tasks.Components.Containers.NormalizedLandmark nose)
        {
            float speed = nose.y - _lastNoseY;
            
            // 冷却控制
            if (speed > pitchSensitivity && Time.time > _nextHitTime)
            {
                _nextHitTime = Time.time + hitCooldown;

                // 1. 触发打击逻辑 (交由 TrackManager 去判断有没有打中)
                gameTrackManager.HandleHitInput(); 
                
                Debug.Log("<color=yellow>有效打击！</color>");
            }
            
            _lastNoseY = nose.y;
        }
    }
}