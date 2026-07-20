using TMPro;
using UnityEngine;

namespace CupPrototype.UI
{
    // 屏幕错误提示入口；核心玩法只调用 Show，未绑定 TMP 时不报错。
    public class DemoMessagePanel : MonoBehaviour
    {
        public static DemoMessagePanel Instance { get; private set; }

        public TextMeshProUGUI messageText;
        public float messageDuration = 2f;

        private float clearAtTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[DemoMessagePanel] Duplicate instance, keeping the latest.", this);
            }

            Instance = this;
            ClearMessage();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (messageText != null && clearAtTime > 0f && Time.time >= clearAtTime)
            {
                ClearMessage();
            }
        }

        public void ShowMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (messageText != null)
            {
                messageText.text = message;
                clearAtTime = Time.time + Mathf.Max(0.1f, messageDuration);
            }

            Debug.Log($"[DemoMessagePanel] {message}", this);
        }

        public void ClearMessage()
        {
            clearAtTime = 0f;
            if (messageText != null)
            {
                messageText.text = string.Empty;
            }
        }
    }
}
