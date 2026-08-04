using CupPrototype.Game;
using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 开发者在 Play 模式录制标准手势模板的 MVP；模板只保存在运行时。
    [DefaultExecutionOrder(-1100)]
    public class GestureTemplateRecorder : MonoBehaviour
    {
        // 录制期间供其它输入系统暂停倒入、转移、拖拽和玩家识别。
        public static bool IsTemplateRecording { get; private set; }

        public KeyCode recordTemplateKey = KeyCode.G;
        public FlairGestureType recordingGestureType = FlairGestureType.Circle;
        public string templateIdPrefix = "DevTemplate";
        public bool debugLogs = true;
        public GestureTemplateLibrary templateLibrary;
        // 勾选后会把录制结果保存成 GestureTemplateAsset；Editor 专用，正式 Build 不会执行 UnityEditor 代码。
        public bool saveRecordedTemplateAsAsset = false;
        public string assetSaveFolder = "Assets/Data/GestureTemplates";

        private readonly List<Vector2> recordedPoints = new List<Vector2>();
        private bool isRecording;
        private int templateCounter;
        // Player 模式下按住 G 时只提示一次，避免每帧刷屏。
        private bool hasLoggedPlayerModeBlock;

        private void Awake()
        {
            if (templateLibrary == null)
            {
                templateLibrary = GetComponent<GestureTemplateLibrary>();
            }
        }

        private void OnDisable()
        {
            isRecording = false;
            IsTemplateRecording = false;
            recordedPoints.Clear();
        }

        private void Update()
        {
            // G 是开发者模板录制入口；Space 玩家手势由 FlairGestureController 独立处理，不受影响。
            if (!DemoModeController.DeveloperModeActive)
            {
                if (isRecording)
                {
                    CancelRecording();
                }

                if (Input.GetKeyDown(recordTemplateKey) && !hasLoggedPlayerModeBlock)
                {
                    Debug.Log("[GestureTemplateRecorder] Template recording is disabled in Player Mode.", this);
                    hasLoggedPlayerModeBlock = true;
                }

                return;
            }

            hasLoggedPlayerModeBlock = false;
            if (isRecording && !Input.GetMouseButton(0))
            {
                FinishRecording();
                return;
            }

            if (Input.GetKey(recordTemplateKey) && Input.GetMouseButtonDown(0))
            {
                StartRecording();
            }

            if (isRecording)
            {
                recordedPoints.Add(Input.mousePosition);
            }
        }

        // 切入 Player 模式时丢弃未完成轨迹，确保不保存资产也不修改运行时模板库。
        private void CancelRecording()
        {
            isRecording = false;
            IsTemplateRecording = false;
            recordedPoints.Clear();
        }

        private void StartRecording()
        {
            recordedPoints.Clear();
            recordedPoints.Add(Input.mousePosition);
            isRecording = true;
            IsTemplateRecording = true;
        }

        private void FinishRecording()
        {
            isRecording = false;
            IsTemplateRecording = false;

            if (templateLibrary == null)
            {
                Debug.LogWarning("[GestureTemplateRecorder] templateLibrary is missing.", this);
                recordedPoints.Clear();
                return;
            }

            // Inspector 前缀为空时使用 DevTemplate，依次生成 DevTemplate_0、DevTemplate_1 等稳定 ID。
            string safePrefix = string.IsNullOrWhiteSpace(templateIdPrefix) ? "DevTemplate" : templateIdPrefix.Trim();
            string templateId = $"{safePrefix}_{templateCounter}";
            GestureTemplateData template = new GestureTemplateData
            {
                templateId = templateId,
                gestureType = recordingGestureType,
                normalizedPoints = GestureTemplateUtility.NormalizePoints(recordedPoints)
            };

            templateLibrary.AddTemplate(template);
            SaveTemplateAssetIfRequested(template);
            if (debugLogs)
            {
                Debug.Log($"[GestureTemplateRecorder] Recorded template: {templateId}, Type={recordingGestureType}", this);
            }

            templateCounter++;
            recordedPoints.Clear();
        }

        private void SaveTemplateAssetIfRequested(GestureTemplateData template)
        {
            if (!saveRecordedTemplateAsAsset)
            {
                return;
            }

#if UNITY_EDITOR
            System.Type saverType = System.Type.GetType("CupPrototype.Flair.GestureTemplateAssetSaver, Assembly-CSharp-Editor");
            System.Reflection.MethodInfo saveMethod = saverType?.GetMethod("SaveTemplateAsset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (saveMethod == null)
            {
                Debug.LogWarning("[GestureTemplateRecorder] GestureTemplateAssetSaver is not available.", this);
                return;
            }

            saveMethod.Invoke(null, new object[] { template, assetSaveFolder });
#else
            Debug.LogWarning("[GestureTemplateRecorder] Saving gesture templates as assets is only available in the Unity Editor.", this);
#endif
        }
    }
}
