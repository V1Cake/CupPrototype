using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 挂在 GameManager 上，负责记录 Space + 鼠标左键轨迹、识别手势并触发工具花式。
    [DefaultExecutionOrder(-1000)]
    public class FlairGestureController : MonoBehaviour
    {
        // 全局花式输入状态：让倒入、转移、拖拽系统知道花式模式正在接管鼠标输入。
        public static bool IsFlairInputActive { get; private set; }

        // 全局花式播放状态：花式动画期间其它输入系统暂停响应。
        public static bool IsFlairPlaying { get; private set; }

        // 用于从鼠标位置发射射线；为空时自动使用 Camera.main。
        public Camera mainCamera;

        // 按住该键时才进入花式手势识别。
        public KeyCode flairModifierKey = KeyCode.Space;

        // 可命中工具的交互层。
        public LayerMask interactableMask = ~0;

        // 是否输出识别和动作日志。
        public bool debugLogs = true;

        // 运行时手势模板库；玩家轨迹会和这里的开发者模板匹配。
        public GestureTemplateLibrary templateLibrary;

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

            GestureMatchResult match = recognizer.RecognizeDetailed(recordedPoints);
            if (!match.isMatched)
            {
                // 识别失败只结束花式输入，不修改材料、容器或普通游戏数据。
                if (debugLogs)
                {
                    Debug.Log("[FlairGestureController] Gesture not recognized.", this);
                }
            }
            else if (debugLogs)
            {
                Debug.Log($"[FlairGestureController] Recognized: {match.gestureType}, Template={match.templateId}", this);
            }

            if (match.isMatched)
            {
                if (activeTool != null &&
                    activeTool.TryGetAction(match, out FlairActionDefinition action))
                {
                    if (debugLogs)
                    {
                        Debug.Log($"[FlairGestureController] Action: {action.actionName}", activeTool);
                    }

                    activeTool.PlayFlair(action);
                }
                else if (debugLogs)
                {
                    Debug.Log($"[FlairGestureController] No action configured for template {match.templateId} or gesture {match.gestureType}", activeTool != null ? activeTool : this);
                }
            }

            activeTool = null;
            recordedPoints.Clear();
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

            if (debugLogs)
            {
                string toolName = tool != null ? tool.name : "None";
                Debug.Log($"[FlairGestureController] Hit: {hit.collider.name}, Tool: {toolName}", this);
            }

            return tool != null;
        }
    }
}
