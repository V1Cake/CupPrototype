using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Game
{
    /// <summary>Demo 的玩家展示模式与开发调试模式。</summary>
    public enum DemoMode
    {
        Player,
        Developer
    }

    /// <summary>
    /// 统一控制开发者入口和调试对象显示；模式切换不修改调饮、评分或手势匹配数据。
    /// </summary>
    public class DemoModeController : MonoBehaviour
    {
        // 场景未配置控制器时保持旧版开发行为，避免破坏已有测试场景。
        private static DemoModeController instance;

        // 启动模式：Player 用于正式展示，Developer 用于模板录制和系统调试。
        [SerializeField] private DemoMode startMode = DemoMode.Developer;
        // 运行时模式切换键。
        [SerializeField] private KeyCode toggleModeKey = KeyCode.F1;
        // 是否允许玩家在运行时切换模式。
        [SerializeField] private bool allowRuntimeToggle = true;
        // Player 模式隐藏的调试面板和开发者专用对象。
        [SerializeField] private GameObject[] developerOnlyObjects;
        // 两种模式都保持显示的订单、评分、提示和消息对象。
        [SerializeField] private GameObject[] playerVisibleObjects;

        public DemoMode CurrentMode { get; private set; }
        public bool IsDeveloperMode => CurrentMode == DemoMode.Developer;
        public static bool DeveloperModeActive => instance == null || instance.IsDeveloperMode;
        public static bool RuntimeToggleAvailable => instance != null && instance.allowRuntimeToggle;
        // 只读暴露分类结果，供 Build 验证器检查，不允许验证器修改场景状态。
        public IReadOnlyList<GameObject> DeveloperOnlyObjects => developerOnlyObjects;
        public IReadOnlyList<GameObject> PlayerVisibleObjects => playerVisibleObjects;

        private void Awake()
        {
            instance = this;
            CurrentMode = startMode;
        }

        private void Start()
        {
            SetMode(startMode);
        }

        private void Update()
        {
            if (allowRuntimeToggle && Input.GetKeyDown(toggleModeKey))
            {
                ToggleMode();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>设置并应用模式；只改变调试入口与对象可见性。</summary>
        public void SetMode(DemoMode mode)
        {
            CurrentMode = mode;
            ApplyMode();
            Debug.Log($"[DemoModeController] Mode changed: {CurrentMode}", this);
        }

        /// <summary>在 Player 与 Developer 之间切换。</summary>
        public void ToggleMode()
        {
            SetMode(IsDeveloperMode ? DemoMode.Player : DemoMode.Developer);
        }

        /// <summary>安全跳过空引用，隐藏或显示整组对象但不销毁它们。</summary>
        public void ApplyMode()
        {
            SetObjectsActive(developerOnlyObjects, IsDeveloperMode);
            SetObjectsActive(playerVisibleObjects, true);
        }

        private static void SetObjectsActive(GameObject[] objects, bool isActive)
        {
            if (objects == null)
            {
                return;
            }

            foreach (GameObject target in objects)
            {
                // 控制器自身必须保持启用，否则切到 Player 后无法再用 F1 切回来。
                if (target != null && (instance == null || target != instance.gameObject))
                {
                    target.SetActive(isActive);
                }
            }
        }
    }
}
