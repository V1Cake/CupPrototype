using CupPrototype.DrinkSystem;
using CupPrototype.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CupPrototype.Game
{
    public enum DemoRoundState
    {
        Ready,
        Mixing,
        Submitted
    }

    // 最小 Demo 回合状态机：只负责订单和状态显示，不阻止原有调饮输入。
    public class DemoRoundManager : MonoBehaviour
    {
        public TargetDrinkData currentTarget;
        public OrderDisplayController orderDisplay;
        public TextMeshProUGUI roundStatusText;

        // 可切换的目标饮品列表，顺序对应数字键 1、2、3。
        // 这是 Inspector 和运行时唯一使用的目标列表，不创建第二份缓存。
        [SerializeField] private List<TargetDrinkData> availableTargets = new List<TargetDrinkData>();
        // 当前目标在列表中的索引。
        [SerializeField] private int currentTargetIndex;
        // 是否启用数字键和下一订单快捷键。
        [SerializeField] private bool enableTargetSwitchKeys = true;
        // 切换到下一目标的按键。
        [SerializeField] private KeyCode nextTargetKey = KeyCode.N;
        // 切换订单时是否复用 R 的重置流程清空当前调饮内容。
        [SerializeField] private bool clearContainersOnTargetSwitch = true;
        // 实际执行评分、反馈和统一重置的现有管理器。
        [SerializeField] private DrinkTestManager drinkTestManager;
        // 是否输出目标列表和切换过程的调试日志。
        [SerializeField] private bool debugLogs = true;

        public DemoRoundState State { get; private set; } = DemoRoundState.Ready;
        // 对外只读的当前评分目标。
        public TargetDrinkData CurrentTarget => currentTarget;
        // 唯一目标列表的只读视图，供 Build 前验证使用，不创建第二份运行时列表。
        public IReadOnlyList<TargetDrinkData> AvailableTargets => availableTargets;

        private void Start()
        {
            ResolveTargetReferences();
            if (debugLogs && DemoModeController.DeveloperModeActive)
            {
                Debug.Log($"[DemoRoundManager] Available target count: {availableTargets?.Count ?? 0}", this);
            }

            if (availableTargets != null && availableTargets.Count > 0)
            {
                currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, availableTargets.Count - 1);
                SetTarget(currentTargetIndex);
                return;
            }

            Debug.LogWarning("[DemoRoundManager] No available targets configured.", this);
            if (drinkTestManager != null)
            {
                drinkTestManager.SetTargetDrink(currentTarget);
            }

            StartRound();
        }

        private void Update()
        {
            if (!enableTargetSwitchKeys)
            {
                return;
            }

            // 数字键直接选择前三个订单；else-if 保证一帧只处理一次目标切换。
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                SetTarget(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                SetTarget(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                SetTarget(2);
            }
            else if (Input.GetKeyDown(nextTargetKey))
            {
                StartNextTarget();
            }
        }

        // 目标切换的统一入口；所有 UI 和评分系统必须接收同一个 selectedTarget 引用。
        public void SetTarget(int index)
        {
            if (availableTargets == null || availableTargets.Count == 0)
            {
                Debug.LogWarning("[DemoRoundManager] No available targets configured.", this);
                return;
            }

            if (index < 0 || index >= availableTargets.Count)
            {
                Debug.LogWarning($"[DemoRoundManager] Target index out of range: {index}.", this);
                return;
            }

            TargetDrinkData selectedTarget = availableTargets[index];
            if (selectedTarget == null)
            {
                Debug.LogWarning($"[DemoRoundManager] Target at index {index} is missing.", this);
                return;
            }

            currentTargetIndex = index;
            currentTarget = selectedTarget;
            ResolveTargetReferences();

            if (clearContainersOnTargetSwitch && drinkTestManager != null)
            {
                drinkTestManager.ResetForTargetSwitch();
            }

            RefreshTargetReferences(selectedTarget);
            Debug.Log($"[DemoRoundManager] Target changed: {OrderDisplayController.GetTargetDisplayName(selectedTarget)}, Index={currentTargetIndex}", this);
        }

        // 按列表循环切换到下一个目标饮品。
        public void StartNextTarget()
        {
            if (availableTargets == null || availableTargets.Count == 0)
            {
                Debug.LogWarning("[DemoRoundManager] No available targets configured.", this);
                return;
            }

            // 必须通过 SetTarget 完成 UI、评分和回合状态的完整同步。
            int nextIndex = (currentTargetIndex + 1) % availableTargets.Count;
            SetTarget(nextIndex);
        }

        // 补全现有 UI 与评分管理器引用，不创建第二套目标管理器。
        private void ResolveTargetReferences()
        {
            if (orderDisplay == null)
            {
                orderDisplay = FindAnyObjectByType<OrderDisplayController>();
            }

            if (drinkTestManager == null)
            {
                drinkTestManager = FindAnyObjectByType<DrinkTestManager>();
            }
        }

        // 使用同一个 selectedTarget 同步订单 UI、F 评分目标、旧反馈和回合状态。
        private void RefreshTargetReferences(TargetDrinkData selectedTarget)
        {
            if (orderDisplay != null)
            {
                orderDisplay.ShowTargetDrink(selectedTarget);
            }

            if (drinkTestManager != null)
            {
                drinkTestManager.SetTargetDrink(selectedTarget);
            }
            else
            {
                Debug.LogWarning("[DemoRoundManager] DrinkTestManager is missing; scoring target was not synchronized.", this);
            }

            if (DemoMessagePanel.Instance != null)
            {
                DemoMessagePanel.Instance.ClearMessage();
            }

            State = DemoRoundState.Mixing;
            SetStatus("Round: Mixing");
        }

        public void StartRound()
        {
            State = DemoRoundState.Mixing;
            if (orderDisplay != null)
            {
                orderDisplay.ShowTargetDrink(currentTarget);
            }

            SetStatus("Round: Mixing");
        }

        public void MarkMixing()
        {
            if (State != DemoRoundState.Submitted)
            {
                State = DemoRoundState.Mixing;
                SetStatus("Round: Mixing");
            }
        }

        public void SubmitRound()
        {
            State = DemoRoundState.Submitted;
            SetStatus("Round: Submitted\nPress R to reset");
        }

        public void ResetRound()
        {
            StartRound();
        }

        public void ShowScoreSummary(string summary)
        {
            SubmitRound();
            if (!string.IsNullOrWhiteSpace(summary))
            {
                SetStatus($"Round: Submitted\n{summary}\nPress R to reset");
            }
        }

        private void SetStatus(string text)
        {
            if (roundStatusText != null)
            {
                roundStatusText.text = text;
            }

            Debug.Log($"[DemoRoundManager] {text}", this);
        }
    }
}
