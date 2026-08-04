using CupPrototype.Game;
using System.Text;
using TMPro;
using UnityEngine;

namespace CupPrototype.UI
{
    // 基础操作提示面板；H 只切换本面板，不影响其它输入。
    public class DemoHintPanelController : MonoBehaviour
    {
        public TextMeshProUGUI hintText;
        public bool showOnStart = true;
        public bool showDeveloperControls = true;
        public KeyCode toggleHintKey = KeyCode.H;

        private bool isVisible;
        private bool lastDeveloperMode;

        private void Start()
        {
            lastDeveloperMode = DemoModeController.DeveloperModeActive;
            if (showOnStart)
            {
                ShowHints();
            }
            else
            {
                HideHints();
            }
        }

        private void Update()
        {
            // 模式变化后立即刷新同一份 Hint Text，不创建第二个提示面板。
            bool isDeveloperMode = DemoModeController.DeveloperModeActive;
            if (isVisible && lastDeveloperMode != isDeveloperMode)
            {
                lastDeveloperMode = isDeveloperMode;
                ShowHints();
            }

            if (Input.GetKeyDown(toggleHintKey))
            {
                ToggleHints();
            }
        }

        public void ShowHints()
        {
            isVisible = true;
            if (hintText == null)
            {
                Debug.LogWarning("[DemoHintPanelController] hintText is missing.", this);
                return;
            }

            hintText.text = BuildHintText();
            // 只切换文本渲染，避免 hintText 与控制器同对象时关闭控制器自身。
            hintText.enabled = true;
        }

        public void HideHints()
        {
            isVisible = false;
            if (hintText != null)
            {
                hintText.enabled = false;
            }
        }

        public void ToggleHints()
        {
            if (isVisible)
            {
                HideHints();
            }
            else
            {
                ShowHints();
            }
        }

        private string BuildHintText()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Controls");
            builder.AppendLine("Click bottle: select ingredient");
            builder.AppendLine("Hold container: pour");
            builder.AppendLine("Shift + click container: select transfer source");
            builder.AppendLine("T: taste");
            builder.AppendLine("F: score");
            builder.AppendLine("R: reset");
            builder.AppendLine("1/2/3: select order");
            builder.AppendLine("N: next order");
            builder.AppendLine("Space + mouse: flair gesture");
            // Player 模式不暴露 G、C 等内部调试入口。
            if (showDeveloperControls && DemoModeController.DeveloperModeActive)
            {
                builder.AppendLine("G + mouse: record developer gesture template");
                builder.AppendLine("C: debug");
            }

            if (DemoModeController.RuntimeToggleAvailable)
            {
                builder.AppendLine("F1: toggle player/developer mode");
            }

            builder.Append("H: toggle help");
            return builder.ToString();
        }
    }
}
