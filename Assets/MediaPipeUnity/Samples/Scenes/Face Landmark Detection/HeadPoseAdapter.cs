using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Mediapipe.Unity;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class HeadPoseAdapter : MonoBehaviour
    {
        [Header("调试阈值")]
        [Range(0.05f, 0.5f)] public float yawThreshold = 0.12f;      
        [Range(0.01f, 0.1f)] public float pitchSensitivity = 0.025f; 

        private float _lastNoseY = 0f;
        private string _lastState = "Center";

        void Update()
        {
            var controller = FindObjectOfType<FaceLandmarkerResultAnnotationController>();
            if (controller == null) return;

            var resultField = controller.GetType().GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            if (resultField == null) return;

            var resultObj = resultField.GetValue(controller);
            if (resultObj == null) return;

            var result = (Mediapipe.Tasks.Vision.FaceLandmarker.FaceLandmarkerResult)resultObj;
            if (result.faceLandmarks == null || result.faceLandmarks.Count == 0) return;

            var landmarks = result.faceLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count < 468) return;

            var nose = landmarks[0];
            var leftEye = landmarks[33];
            var rightEye = landmarks[263];

            DetectYaw(nose, leftEye, rightEye);
            DetectPitch(nose);
        }

        void DetectYaw(Mediapipe.Tasks.Components.Containers.NormalizedLandmark nose, 
                       Mediapipe.Tasks.Components.Containers.NormalizedLandmark left, 
                       Mediapipe.Tasks.Components.Containers.NormalizedLandmark right)
        {
            float eyeCenter = (left.x + right.x) / 2f;
            float eyeWidth = Mathf.Abs(right.x - left.x);
            float offset = (nose.x - eyeCenter) / eyeWidth;

            string currentState = "Center";
            if (offset < -yawThreshold) currentState = "LEFT (RED)";
            else if (offset > yawThreshold) currentState = "RIGHT (BLUE)";
            else currentState = "CENTER (WHITE)";

            if (currentState != _lastState)
            {
                Debug.Log("[转头测试] 当前状态: " + currentState);
                _lastState = currentState;
            }
        }

        void DetectPitch(Mediapipe.Tasks.Components.Containers.NormalizedLandmark nose)
        {
            float speed = nose.y - _lastNoseY;

            if (speed > pitchSensitivity)
            {
                Debug.Log("[动作测试] 检测到点头！瞬时速度: " + speed);
            }

            _lastNoseY = nose.y;
        }
    }
}