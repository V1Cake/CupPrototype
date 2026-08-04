using System.Collections.Generic;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.Game;
using CupPrototype.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CupPrototype.Validation
{
    /// <summary>
    /// 在 Play 或 Build 前只读检查 Demo 场景的关键对象、数据和 UI 引用；不会自动创建、删除或启停对象。
    /// </summary>
    public class BuildReadinessValidator : MonoBehaviour
    {
        // 进入 Play 时自动执行一次完整检查。
        [SerializeField] private bool validateOnStart = true;
        // 默认把禁用对象也纳入检查，避免遗漏 Player/Developer 模式切换组。
        [SerializeField] private bool includeInactiveObjects = true;
        // 默认只报告问题，减少正式演示时的成功日志噪音。
        [SerializeField] private bool logSuccessfulChecks = false;

        private int warningCount;

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateBuildReadiness();
            }
        }

        /// <summary>执行完整的 Build 就绪检查；所有检查均为只读，不修改 Inspector 或场景状态。</summary>
        public void ValidateBuildReadiness()
        {
            warningCount = 0;

            Camera[] cameras = FindAll<Camera>();
            int mainCameraCount = 0;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].CompareTag("MainCamera"))
                {
                    mainCameraCount++;
                }
            }

            RequireExactlyOne(mainCameraCount, "Main Camera");
            RequireExactlyOne(FindAll<EventSystem>().Length, "EventSystem");
            RequireExactlyOne(CountNamedObjects("GameManager"), "GameManager");

            DemoModeController[] modeControllers = FindAll<DemoModeController>();
            DrinkTestManager[] testManagers = FindAll<DrinkTestManager>();
            DemoRoundManager[] roundManagers = FindAll<DemoRoundManager>();
            GestureTemplateLibrary[] libraries = FindAll<GestureTemplateLibrary>();

            RequireExactlyOne(modeControllers.Length, "DemoModeController");
            RequireExactlyOne(testManagers.Length, "DrinkTestManager");
            RequireExactlyOne(roundManagers.Length, "DemoRoundManager");
            RequireExactlyOne(libraries.Length, "GestureTemplateLibrary");

            ValidateContainers(testManagers.Length == 1 ? testManagers[0] : null);
            ValidateTargets(roundManagers.Length == 1 ? roundManagers[0] : null);
            ValidateTemplates(libraries.Length == 1 ? libraries[0] : null);
            ValidateUi(modeControllers.Length == 1 ? modeControllers[0] : null,
                roundManagers.Length == 1 ? roundManagers[0] : null);
            ValidateEditorOnlyConfiguration();

            if (logSuccessfulChecks)
            {
                Debug.Log($"[BuildReadinessValidator] Validation complete. Warnings={warningCount}.", this);
            }
        }

        /// <summary>确认评分杯、Jigger 与 Shaker 均存在，且评分引用明确指向 FinalGlass。</summary>
        private void ValidateContainers(DrinkTestManager testManager)
        {
            DrinkContainer[] containers = FindAll<DrinkContainer>();
            int finalGlassCount = 0;
            int jiggerCount = 0;
            int shakerCount = 0;

            for (int i = 0; i < containers.Length; i++)
            {
                switch (containers[i].containerType)
                {
                    case DrinkContainer.ContainerType.FinalGlass:
                        finalGlassCount++;
                        break;
                    case DrinkContainer.ContainerType.Jigger:
                        jiggerCount++;
                        break;
                    case DrinkContainer.ContainerType.Shaker:
                        shakerCount++;
                        break;
                }
            }

            RequireAtLeastOne(finalGlassCount, "FinalGlass container");
            RequireAtLeastOne(jiggerCount, "Jigger container");
            RequireAtLeastOne(shakerCount, "Shaker container");

            if (testManager == null || testManager.scoringContainer == null)
            {
                Warn("DrinkTestManager.scoringContainer is missing.");
            }
            else if (testManager.scoringContainer.containerType != DrinkContainer.ContainerType.FinalGlass)
            {
                Warn("DrinkTestManager.scoringContainer must reference a FinalGlass container.");
            }
            else
            {
                Success("Scoring container references FinalGlass.");
            }
        }

        /// <summary>检查唯一目标列表至少包含一个有效 TargetDrinkData，不建立第二份目标缓存。</summary>
        private void ValidateTargets(DemoRoundManager roundManager)
        {
            if (roundManager == null)
            {
                return;
            }

            IReadOnlyList<TargetDrinkData> targets = roundManager.AvailableTargets;
            int validCount = 0;
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null)
                    {
                        validCount++;
                    }
                    else
                    {
                        Warn($"DemoRoundManager.availableTargets[{i}] is missing.");
                    }
                }
            }

            RequireAtLeastOne(validCount, "valid TargetDrinkData");
        }

        /// <summary>检查模板资产具备 ID 和标准化点；只报告问题，不修正模板数据。</summary>
        private void ValidateTemplates(GestureTemplateLibrary library)
        {
            if (library == null)
            {
                return;
            }

            int validCount = 0;
            if (library.templateAssets != null)
            {
                for (int i = 0; i < library.templateAssets.Count; i++)
                {
                    GestureTemplateAsset asset = library.templateAssets[i];
                    if (asset != null && !string.IsNullOrWhiteSpace(asset.templateId) &&
                        asset.normalizedPoints != null && asset.normalizedPoints.Count > 0)
                    {
                        validCount++;
                    }
                }
            }

            RequireAtLeastOne(validCount, "valid GestureTemplateAsset");
        }

        /// <summary>
        /// 所有关键 UI 必须有文本引用，并归入 Player 可见组；订单和评分不得放进开发者专用层级。
        /// </summary>
        private void ValidateUi(DemoModeController modeController, DemoRoundManager roundManager)
        {
            OrderDisplayController orderDisplay = FindSingleForReference<OrderDisplayController>("OrderDisplayController");
            DrinkDebugUI debugUi = FindSingleForReference<DrinkDebugUI>("DrinkDebugUI");
            DemoHintPanelController hintPanel = FindSingleForReference<DemoHintPanelController>("DemoHintPanelController");
            DemoMessagePanel messagePanel = FindSingleForReference<DemoMessagePanel>("DemoMessagePanel");

            CheckUiReference(orderDisplay != null ? orderDisplay.orderText : null, "Order Text", modeController);
            CheckUiReference(debugUi != null ? debugUi.ScoreText : null, "Score Text", modeController);
            CheckUiReference(roundManager != null ? roundManager.roundStatusText : null, "Round Status Text", modeController);
            CheckUiReference(hintPanel != null ? hintPanel.hintText : null, "Hint Text", modeController);
            CheckUiReference(messagePanel != null ? messagePanel.messageText : null, "Message Text", modeController);

            // 禁止把玩家必须看到的订单和评分放在 DebugPanel 等 developerOnly 根对象下。
            CheckNotDeveloperOnly(orderDisplay != null ? orderDisplay.orderText : null, "Order Text", modeController);
            CheckNotDeveloperOnly(debugUi != null ? debugUi.ScoreText : null, "Score Text", modeController);
        }

        private void CheckUiReference(Component component, string label, DemoModeController modeController)
        {
            if (component == null)
            {
                Warn(label + " reference is missing.");
                return;
            }

            if (modeController != null && !IsCoveredBy(component.transform, modeController.PlayerVisibleObjects))
            {
                Warn(label + " is not covered by DemoModeController.playerVisibleObjects.");
            }
            else
            {
                Success(label + " is configured for Player mode.");
            }
        }

        private void CheckNotDeveloperOnly(Component component, string label, DemoModeController modeController)
        {
            if (component != null && modeController != null &&
                IsCoveredBy(component.transform, modeController.DeveloperOnlyObjects))
            {
                Warn(label + " is inside DemoModeController.developerOnlyObjects and will disappear in Player mode.");
            }
        }

        /// <summary>检查目标是否等于或位于配置根对象之下，用于验证 UI 模式分类。</summary>
        private static bool IsCoveredBy(Transform target, IReadOnlyList<GameObject> roots)
        {
            if (target == null || roots == null)
            {
                return false;
            }

            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] != null && (target == roots[i].transform || target.IsChildOf(roots[i].transform)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Editor 保存逻辑已由条件编译隔离；Player 中仅提醒被勾选但不可用的保存选项。</summary>
        private void ValidateEditorOnlyConfiguration()
        {
#if !UNITY_EDITOR
            GestureTemplateRecorder[] recorders = FindAll<GestureTemplateRecorder>();
            for (int i = 0; i < recorders.Length; i++)
            {
                if (recorders[i].saveRecordedTemplateAsAsset)
                {
                    Warn("GestureTemplateRecorder asset saving is Editor-only and is ignored in Player builds.");
                }
            }
#else
            Success("UnityEditor dependencies are isolated to Editor code or conditional compilation.");
#endif
        }

        private T FindSingleForReference<T>(string label) where T : Component
        {
            T[] objects = FindAll<T>();
            if (objects.Length == 0)
            {
                Warn(label + " is missing.");
                return null;
            }

            if (objects.Length > 1)
            {
                Warn($"Expected one {label}, found {objects.Length}.");
            }

            return objects[0];
        }

        private int CountNamedObjects(string objectName)
        {
            Transform[] transforms = FindAll<Transform>();
            int count = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                {
                    count++;
                }
            }

            return count;
        }

        private T[] FindAll<T>() where T : Object
        {
            FindObjectsInactive inactive = includeInactiveObjects
                ? FindObjectsInactive.Include
                : FindObjectsInactive.Exclude;
            return FindObjectsByType<T>(inactive, FindObjectsSortMode.None);
        }

        private void RequireExactlyOne(int count, string label)
        {
            if (count != 1)
            {
                Warn($"Expected exactly one {label}, found {count}.");
            }
            else
            {
                Success(label + " count is valid.");
            }
        }

        private void RequireAtLeastOne(int count, string label)
        {
            if (count == 0)
            {
                Warn("No " + label + " configured.");
            }
            else
            {
                Success($"Found {count} {label}.");
            }
        }

        private void Warn(string message)
        {
            warningCount++;
            Debug.LogWarning("[BuildReadinessValidator] " + message, this);
        }

        private void Success(string message)
        {
            if (logSuccessfulChecks)
            {
                Debug.Log("[BuildReadinessValidator] " + message, this);
            }
        }
    }
}
