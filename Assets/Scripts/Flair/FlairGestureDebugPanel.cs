using CupPrototype.Game;
using System.Text;
using TMPro;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 显示最近一次手势识别结果；未绑定文本时只保留 Console 输出。
    public class FlairGestureDebugPanel : MonoBehaviour
    {
        public static FlairGestureDebugPanel Instance { get; private set; }

        public TextMeshProUGUI gestureDebugText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[FlairGestureDebugPanel] Duplicate instance, keeping the latest.", this);
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(FlairGestureDebugInfo info)
        {
            // Player 模式隐藏 templateId、distance 和匹配模式等内部识别细节。
            if (info == null || !DemoModeController.DeveloperModeActive)
            {
                return;
            }

            string text = Format(info);
            if (gestureDebugText != null)
            {
                gestureDebugText.text = text;
            }

            Debug.Log($"[FlairGestureDebugPanel]\n{text}", this);
        }

        private static string Format(FlairGestureDebugInfo info)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Gesture");

            if (!string.IsNullOrWhiteSpace(info.failureReason))
            {
                builder.AppendLine($"Failed: {info.failureReason}");
            }

            if (info.hasResult)
            {
                builder.AppendLine($"Type: {info.gestureType}");
                builder.AppendLine($"Template: {Display(info.templateId)}");
                builder.AppendLine($"Distance: {info.distance:0.00}");
            }

            builder.AppendLine($"Tool: {Display(info.activeToolName)}");
            builder.AppendLine($"Action: {Display(info.actionName)}");
            builder.AppendLine($"Mode: {Display(info.matchMode)}");
            return builder.ToString();
        }

        private static string Display(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }
}
