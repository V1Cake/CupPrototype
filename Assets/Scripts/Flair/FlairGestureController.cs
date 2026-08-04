using CupPrototype.UI;
using System.Collections.Generic;
using CupPrototype.Game;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 挂在 GameManager 上，负责记录 Space + 鼠标轨迹、识别手势并触发工具花式。
    [DefaultExecutionOrder(-1000)]
    public class FlairGestureController : MonoBehaviour
    {
        // 全局花式输入状态：让倒入、转移、拖拽系统暂停响应鼠标输入。
        public static bool IsFlairInputActive { get; private set; }

        // 全局花式播放状态：花式动画期间其它输入系统暂停响应。
        public static bool IsFlairPlaying { get; private set; }

        public Camera mainCamera;
        public KeyCode flairModifierKey = KeyCode.Space;
        public LayerMask interactableMask = ~0;
        public bool debugLogs = true;
        public GestureTemplateLibrary templateLibrary;
        public FlairGestureDebugPanel debugPanel;

        private FlairableTool activeTool;
        private List<Vector2> recordedPoints;
        private bool isRecording;
        private FlairGestureRecognizer recognizer;

        public static void SetFlairPlaying(bool isPlaying)
        {
            IsFlairPlaying = isPlaying;
        }

        private void Awake()
        {
            IsFlairInputActive = false;
            IsFlairPlaying = false;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (templateLibrary == null)
            {
                templateLibrary = GetComponent<GestureTemplateLibrary>();
            }

            recordedPoints = new List<Vector2>();
            recognizer = new FlairGestureRecognizer(templateLibrary);
        }

        private void OnDisable()
        {
            IsFlairInputActive = false;
            IsFlairPlaying = false;
            isRecording = false;
            activeTool = null;
            recordedPoints?.Clear();
        }

        private void Update()
        {
            if (isRecording && !Input.GetMouseButton(0))
            {
                FinishRecording();
                return;
            }

            if (Input.GetKey(flairModifierKey) && Input.GetMouseButtonDown(0))
            {
                TryStartRecording();
            }

            if (isRecording)
            {
                recordedPoints.Add(Input.mousePosition);
            }

            if (isRecording && Input.GetMouseButtonUp(0))
            {
                FinishRecording();
            }
        }

        private void TryStartRecording()
        {
            if (mainCamera == null || isRecording || GestureTemplateRecorder.IsTemplateRecording)
            {
                return;
            }

            TryGetToolUnderMouse(out FlairableTool tool);
            if (tool != null && tool.IsPlayingFlair)
            {
                Debug.Log($"[FlairableTool] Flair already playing on {tool.name}", tool);
                return;
            }

            if (IsFlairPlaying)
            {
                return;
            }

            activeTool = tool;
            recordedPoints.Clear();
            recordedPoints.Add(Input.mousePosition);
            isRecording = true;
            IsFlairInputActive = true;
        }

        private void FinishRecording()
        {
            isRecording = false;
            IsFlairInputActive = false;

            bool hasTemplates = templateLibrary != null && templateLibrary.GetTemplates().Count > 0;
            GestureMatchResult match = recognizer.RecognizeDetailed(recordedPoints);
            FlairGestureDebugInfo debugInfo = new FlairGestureDebugInfo
            {
                hasResult = match.isMatched,
                activeToolName = activeTool != null ? activeTool.name : "None",
                templateId = match.templateId,
                gestureType = match.gestureType,
                distance = match.distance,
                matchMode = match.isMatched ? "NoAction" : "NoMatch",
                failureReason = match.isMatched ? string.Empty : (hasTemplates ? "No matching template" : "No gesture templates")
            };

            if (!match.isMatched)
            {
                if (debugLogs && DemoModeController.DeveloperModeActive)
                {
                    Debug.Log("[FlairGestureController] Gesture not recognized.", this);
                }

                ShowMessage(DemoModeController.DeveloperModeActive
                    ? (hasTemplates ? "No gesture template matched" : "No gesture templates")
                    : "Gesture not recognized");
            }
            else if (debugLogs && DemoModeController.DeveloperModeActive)
            {
                Debug.Log($"[FlairGestureController] Recognized: {match.gestureType}, Template={match.templateId}, Distance={match.distance:0.00}", this);
            }

            if (match.isMatched)
            {
                string matchMode = "NoAction";
                if (activeTool != null &&
                    activeTool.TryGetAction(match, out FlairActionDefinition action, out matchMode))
                {
                    debugInfo.actionName = action.actionName;
                    debugInfo.matchMode = matchMode;
                    if (debugLogs && DemoModeController.DeveloperModeActive)
                    {
                        Debug.Log($"[FlairGestureController] Action: {action.actionName}, Mode: {matchMode}", activeTool);
                    }

                    activeTool.PlayFlair(action);
                    if (!DemoModeController.DeveloperModeActive)
                    {
                        ShowMessage("Flair recognized");
                    }
                }
                else
                {
                    string failureReason = matchMode == "FallbackDisabled"
                        ? "Fallback disabled"
                        : "No action configured";

                    debugInfo.matchMode = matchMode;
                    debugInfo.failureReason = failureReason;
                    ShowMessage(DemoModeController.DeveloperModeActive
                        ? failureReason
                        : "This gesture is not available for this tool");

                    if (debugLogs && DemoModeController.DeveloperModeActive)
                    {
                        Debug.Log($"[FlairGestureController] {failureReason} for template {match.templateId} or gesture {match.gestureType}", activeTool != null ? (Object)activeTool : this);
                    }
                }
            }

            ShowDebugInfo(debugInfo);
            activeTool = null;
            recordedPoints.Clear();
        }

        private void ShowDebugInfo(FlairGestureDebugInfo info)
        {
            FlairGestureDebugPanel panel = debugPanel != null ? debugPanel : FlairGestureDebugPanel.Instance;
            if (panel != null)
            {
                panel.Show(info);
            }
            else if (debugLogs && DemoModeController.DeveloperModeActive)
            {
                Debug.Log($"[FlairGestureController] Mode={info.matchMode}, Failure={info.failureReason}", this);
            }
        }

        private void ShowMessage(string message)
        {
            if (DemoMessagePanel.Instance != null)
            {
                DemoMessagePanel.Instance.ShowMessage(message);
            }
        }

        private bool TryGetToolUnderMouse(out FlairableTool tool)
        {
            tool = null;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interactableMask))
            {
                return false;
            }

            tool = hit.collider.GetComponent<FlairableTool>();
            if (tool == null)
            {
                tool = hit.collider.GetComponentInParent<FlairableTool>();
            }

            if (debugLogs && DemoModeController.DeveloperModeActive)
            {
                string toolName = tool != null ? tool.name : "None";
                Debug.Log($"[FlairGestureController] Hit: {hit.collider.name}, Tool: {toolName}", this);
            }

            return tool != null;
        }
    }
}
