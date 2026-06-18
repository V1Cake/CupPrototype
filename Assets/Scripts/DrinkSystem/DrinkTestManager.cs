using CupPrototype.Interaction;
using CupPrototype.UI;
using System;
using System.Text;
using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 饮品测试管理器 =====
    // 原型阶段的输入中枢：选择材料、持续倒入、试味、评分、清空和 Debug UI 更新。
    public class DrinkTestManager : MonoBehaviour
    {
        // ===== 输入状态机 =====
        // Idle：无选择；IngredientSelected：已选材料；PouringIngredient：正在倒入材料。
        // ContainerSourceSelected：已选转移源；TransferringContainer：正在容器转移。
        private enum InputMode
        {
            Idle,
            IngredientSelected,
            PouringIngredient,
            ContainerSourceSelected,
            TransferringContainer
        }

        // ===== Inspector 绑定参数 =====
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask clickableLayers = ~0;
        [SerializeField] private float raycastDistance = 1000f;
        [SerializeField] private TargetDrinkData targetDrink;
        [SerializeField] private DrinkDebugUI debugUI;
        // scoringContainer 用于在多容器场景中明确指定按 F 评分的最终杯。
        public DrinkContainer scoringContainer;
        // containerTransferRatePerSecond 控制量杯向其他容器转移时的每秒转移量。
        public float containerTransferRatePerSecond = 30f;

        // ===== 当前选择与倒入状态 =====
        private PourableIngredient selectedIngredient;
        private PourableIngredient currentSelectedIngredient;
        private DrinkContainer selectedSourceContainer;
        private DrinkContainer currentPourContainer;
        private DrinkContainer currentTransferTargetContainer;
        private bool isPouring;
        private bool isTransferringContainer;
        private bool promptedMissingIngredientThisPress;
        private InputMode currentMode = InputMode.Idle;

        // ===== 给 DragController 查询的全局选择状态 =====
        // 有材料选中时，杯子左键优先用于倒入；没有材料时杯子可以拖动。
        public static bool HasSelectedIngredient { get; private set; }

        // ===== 生命周期：初始化相机和静态状态 =====
        private void Awake()
        {
            HasSelectedIngredient = false;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        // ===== 每帧输入入口 =====
        // T=试味，F=评分，R=清空；鼠标左键负责选择材料和持续倒入。
        private void Update()
        {
            HandleKeyboardShortcuts();

            if (targetCamera == null)
            {
                return;
            }

            HandleMouseInput();
        }

        // ===== 键盘快捷键处理 =====
        private void HandleKeyboardShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                TasteCurrentDrink();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                ScoreCurrentDrink();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ClearCurrentDrink();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                PrintAllContainerDebugInfo();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearSelectedSourceContainer();
            }
        }

        // ===== 鼠标输入处理 =====
        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                promptedMissingIngredientThisPress = false;
                HandleMouseDown();
            }

            if (Input.GetMouseButton(0))
            {
                if (currentSelectedIngredient != null)
                {
                    ContinueIngredientPour();
                }
                else
                {
                    ContinueContainerTransfer();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (currentMode == InputMode.PouringIngredient)
                {
                    StopIngredientPour();
                }
                else if (currentMode == InputMode.TransferringContainer)
                {
                    StopContainerTransfer();
                }

                promptedMissingIngredientThisPress = false;
            }
        }

        // ===== 鼠标按下：判断点击对象类型 =====
        private void HandleMouseDown()
        {
            if (!TryGetObjectUnderMouse(out RaycastHit hit))
            {
                return;
            }

            DrinkContainer drinkContainer = hit.collider.GetComponentInParent<DrinkContainer>();
            if (IsShiftPressed() && currentSelectedIngredient == null && drinkContainer != null)
            {
                HandleContainerSourceSelection(drinkContainer);
                return;
            }

            PourableIngredient pourableIngredient = hit.collider.GetComponentInParent<PourableIngredient>();
            if (pourableIngredient != null)
            {
                HandleIngredientSelection(pourableIngredient);
                return;
            }

            if (drinkContainer != null)
            {
                if (selectedIngredient == null)
                {
                    return;
                }

                StartIngredientPour(drinkContainer);
            }
        }

        // ===== 选择或取消选择材料 =====
        // 再次点击同一个材料瓶会取消选择，点击新材料瓶会切换高亮。
        private void HandleIngredientSelection(PourableIngredient clickedIngredient)
        {
            if (currentSelectedIngredient == clickedIngredient)
            {
                ClearSelectedIngredient();
                return;
            }

            if (currentSelectedIngredient != null && currentSelectedIngredient != clickedIngredient)
            {
                SelectionHighlight previousHighlight = currentSelectedIngredient.GetComponent<SelectionHighlight>();
                if (previousHighlight != null)
                {
                    previousHighlight.SetHighlighted(false);
                }
            }

            ClearSelectedSourceContainer();
            selectedIngredient = clickedIngredient;
            currentSelectedIngredient = clickedIngredient;
            UpdateDragBlockState();
            SetInputMode(InputMode.IngredientSelected);

            SelectionHighlight currentHighlight = currentSelectedIngredient.GetComponent<SelectionHighlight>();
            if (currentHighlight != null)
            {
                currentHighlight.SetHighlighted(true);
            }

            string ingredientName = selectedIngredient.ingredientData != null
                ? selectedIngredient.ingredientData.ingredientName
                : "Ingredient data not assigned";

            Debug.Log($"[DrinkTestManager] Selected ingredient: {ingredientName}", selectedIngredient);
            UpdateSelectedIngredientUI();
        }

        // ===== 选择或取消源容器 =====
        // Shift + 左键点击容器时进入转移源选择模式。
        private void HandleContainerSourceSelection(DrinkContainer clickedContainer)
        {
            if (clickedContainer == null)
            {
                return;
            }

            if (selectedSourceContainer == clickedContainer)
            {
                ClearSelectedSourceContainer();
                return;
            }

            selectedSourceContainer = clickedContainer;
            UpdateDragBlockState();
            SetInputMode(InputMode.ContainerSourceSelected);
            Debug.Log($"[DrinkTestManager] Transfer source selected: {selectedSourceContainer.DisplayName}", selectedSourceContainer);
            UpdateTransferSourceUI();
        }

        // ===== 更新当前材料 UI =====
        private void UpdateSelectedIngredientUI()
        {
            if (debugUI != null)
            {
                string ingredientName = selectedIngredient != null && selectedIngredient.ingredientData != null
                    ? selectedIngredient.ingredientData.ingredientName
                    : "None";

                debugUI.SetSelectedIngredient(ingredientName);
            }
            else
            {
                Debug.LogWarning("[DrinkTestManager] debugUI is null, cannot update selected ingredient UI.", this);
            }
        }

        // ===== 鼠标按住：持续倒入 =====
        // 只有当前有选中材料，并且鼠标指向 DrinkContainer 时才会持续加料。
        private void ContinueIngredientPour()
        {
            if (currentMode == InputMode.Idle || selectedIngredient == null)
            {
                return;
            }

            if (!TryGetDrinkContainerUnderMouse(out DrinkContainer drinkContainer))
            {
                StopIngredientPour();
                return;
            }

            if (!drinkContainer.CanAdd(0.001f))
            {
                StopIngredientPour();
                return;
            }

            if (currentPourContainer != drinkContainer || !isPouring)
            {
                StartIngredientPour(drinkContainer);
            }

            float amount = selectedIngredient.pourRatePerSecond * Time.deltaTime;
            drinkContainer.AddIngredient(selectedIngredient.ingredientData, amount, false);

            if (debugUI != null)
            {
                debugUI.SetVolume(drinkContainer.DisplayName, drinkContainer.CurrentVolume, drinkContainer.MaxVolume);
            }
        }

        // ===== 鼠标按住：持续转移容器内容 =====
        // 只有没有材料选中、且已有源容器时才会执行。
        private void ContinueContainerTransfer()
        {
            if (selectedSourceContainer == null ||
                currentMode == InputMode.Idle ||
                currentMode == InputMode.IngredientSelected ||
                currentSelectedIngredient != null)
            {
                return;
            }

            if (selectedSourceContainer.IsEmpty())
            {
                ClearSelectedSourceContainer();
                return;
            }

            if (!TryGetDrinkContainerUnderMouse(out DrinkContainer targetContainer) ||
                targetContainer == selectedSourceContainer)
            {
                StopContainerTransfer();
                return;
            }

            if (targetContainer.IsFull())
            {
                StopContainerTransfer();
                return;
            }

            if (!isTransferringContainer || currentTransferTargetContainer != targetContainer)
            {
                StartContainerTransfer(targetContainer);
            }

            bool transferred = selectedSourceContainer.TransferTo(
                targetContainer,
                containerTransferRatePerSecond * Time.deltaTime);

            if (transferred && debugUI != null)
            {
                debugUI.SetVolume(targetContainer.DisplayName, targetContainer.CurrentVolume, targetContainer.MaxVolume);
            }

            if (selectedSourceContainer == null || selectedSourceContainer.IsEmpty())
            {
                ClearSelectedSourceContainer();
            }
        }

        // ===== 鼠标射线工具 =====
        private bool TryGetObjectUnderMouse(out RaycastHit hit)
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, raycastDistance, clickableLayers);
        }

        // ===== 鼠标下方杯子查询 =====
        private bool TryGetDrinkContainerUnderMouse(out DrinkContainer drinkContainer)
        {
            drinkContainer = null;

            if (!TryGetObjectUnderMouse(out RaycastHit hit))
            {
                return false;
            }

            drinkContainer = hit.collider.GetComponentInParent<DrinkContainer>();
            return drinkContainer != null;
        }

        // ===== 开始倒入 =====
        private void StartIngredientPour(DrinkContainer targetContainer)
        {
            if (selectedIngredient == null || targetContainer == null)
            {
                return;
            }

            if (isPouring && currentPourContainer == targetContainer)
            {
                return;
            }

            currentPourContainer = targetContainer;
            isPouring = true;
            SetInputMode(InputMode.PouringIngredient);

            string ingredientName = selectedIngredient != null && selectedIngredient.ingredientData != null
                ? selectedIngredient.ingredientData.ingredientName
                : "Ingredient data not assigned";

            Debug.Log($"[DrinkTestManager] Start pouring {ingredientName} into {targetContainer.DisplayName}", targetContainer);
        }

        // ===== 开始容器转移 =====
        private void StartContainerTransfer(DrinkContainer targetContainer)
        {
            if (selectedSourceContainer == null || targetContainer == null || targetContainer == selectedSourceContainer)
            {
                return;
            }

            currentTransferTargetContainer = targetContainer;
            isTransferringContainer = true;
            SetInputMode(InputMode.TransferringContainer);
        }

        // ===== 停止倒入 =====
        private void StopIngredientPour()
        {
            if (!isPouring)
            {
                currentPourContainer = null;
                SetInputMode(selectedIngredient != null ? InputMode.IngredientSelected : InputMode.Idle);
                return;
            }

            Debug.Log("[DrinkTestManager] Stop pouring.", currentPourContainer);
            currentPourContainer = null;
            isPouring = false;
            SetInputMode(selectedIngredient != null ? InputMode.IngredientSelected : InputMode.Idle);
        }

        // ===== 停止容器转移 =====
        private void StopContainerTransfer()
        {
            if (!isTransferringContainer)
            {
                currentTransferTargetContainer = null;
                SetInputMode(selectedSourceContainer != null ? InputMode.ContainerSourceSelected : InputMode.Idle);
                return;
            }

            currentTransferTargetContainer = null;
            isTransferringContainer = false;
            SetInputMode(selectedSourceContainer != null ? InputMode.ContainerSourceSelected : InputMode.Idle);
        }

        // ===== 未选择材料提示 =====
        // 防止按住杯子时每帧重复输出提示。
        private void PromptSelectIngredientOnce(UnityEngine.Object context)
        {
            if (promptedMissingIngredientThisPress)
            {
                return;
            }

            Debug.Log("Please select an ingredient first.", context);
            promptedMissingIngredientThisPress = true;
        }

        // ===== 清除当前材料选择 =====
        private void ClearSelectedIngredient()
        {
            if (currentSelectedIngredient != null)
            {
                SelectionHighlight currentHighlight = currentSelectedIngredient.GetComponent<SelectionHighlight>();
                if (currentHighlight != null)
                {
                    currentHighlight.SetHighlighted(false);
                }
            }

            selectedIngredient = null;
            currentSelectedIngredient = null;
            UpdateDragBlockState();
            StopIngredientPour();
            SetInputMode(InputMode.Idle);

            Debug.Log("[DrinkTestManager] Ingredient selection cleared.", this);
            UpdateSelectedIngredientUI();
        }

        // ===== 清除源容器选择 =====
        private void ClearSelectedSourceContainer()
        {
            if (selectedSourceContainer == null)
            {
                return;
            }

            selectedSourceContainer = null;
            StopContainerTransfer();
            UpdateDragBlockState();
            SetInputMode(currentSelectedIngredient != null ? InputMode.IngredientSelected : InputMode.Idle);

            Debug.Log("[DrinkTestManager] Transfer source cleared.", this);
            UpdateTransferSourceUI();
        }

        // ===== 更新转移源 UI =====
        private void UpdateTransferSourceUI()
        {
            if (debugUI != null)
            {
                debugUI.SetTransferSource(selectedSourceContainer != null
                    ? selectedSourceContainer.DisplayName
                    : "None");
            }
        }

        // ===== 更新拖拽阻断状态 =====
        // DragController 通过这个静态值判断杯子当前是否应让位给倒入/转移输入。
        private void UpdateDragBlockState()
        {
            HasSelectedIngredient = currentSelectedIngredient != null || selectedSourceContainer != null;
        }

        // ===== Shift 判断 =====
        private bool IsShiftPressed()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        // ===== 输入状态切换 =====
        private void SetInputMode(InputMode nextMode)
        {
            if (currentMode == nextMode)
            {
                return;
            }

            InputMode previousMode = currentMode;
            currentMode = nextMode;
            Debug.Log($"[DrinkTestManager] Mode changed: {previousMode} -> {currentMode}", this);
        }

        // ===== 快捷键 T：试味 =====
        private void TasteCurrentDrink()
        {
            DrinkContainer drinkContainer = FindFirstObjectByType<DrinkContainer>();
            if (drinkContainer == null)
            {
                Debug.Log("No DrinkContainer found in the scene.", this);
                return;
            }

            FlavorProfile profile = drinkContainer.GetCurrentFlavorProfile();
            string feedback = TasteFeedbackSystem.GenerateFeedback(profile);

            Debug.Log($"Current flavor: {profile.ToDebugString()}\nTaste: {feedback}", drinkContainer);

            if (debugUI != null)
            {
                debugUI.SetTasteFeedback(feedback);
            }
        }

        // ===== 快捷键 F：评分 =====
        private void ScoreCurrentDrink()
        {
            if (targetDrink == null)
            {
                Debug.Log("Please assign a Target Drink in DrinkTestManager.", this);
                return;
            }

            DrinkContainer drinkContainer = GetScoringContainer();
            if (drinkContainer == null)
            {
                Debug.Log("No DrinkContainer found in the scene.", this);
                return;
            }

            Debug.Log($"[DrinkTestManager] Scoring container: {drinkContainer.DisplayName}, Ingredients: {drinkContainer.GetIngredientDebugString()}", drinkContainer);

            DrinkScoreResult scoreResult = DrinkScoreSystem.ScoreDrink(drinkContainer, targetDrink);
            string missingIngredients = scoreResult.missingRequiredIngredients.Count > 0
                ? string.Join(", ", scoreResult.missingRequiredIngredients)
                : "None";

            Debug.Log(
                $"Target Drink: {targetDrink.drinkName}\n" +
                $"Total: {scoreResult.totalScore:0.0}/100\n" +
                $"Flavor: {scoreResult.flavorScore:0.0}/60\n" +
                $"Volume: {scoreResult.volumeScore:0.0}/20\n" +
                $"Ingredient: {scoreResult.ingredientScore:0.0}/20\n" +
                $"Missing: {missingIngredients}\n" +
                $"Feedback: {scoreResult.feedbackText}",
                drinkContainer);

            if (debugUI != null)
            {
                debugUI.SetScoreResult(scoreResult);
            }
        }

        // ===== 评分容器选择 =====
        // 多容器场景中，优先使用 Inspector 指定的 scoringContainer；
        // 如果未指定，则优先寻找 containerType 为 FinalGlass 的容器，最后才 fallback 到第一个容器。
        private DrinkContainer GetScoringContainer()
        {
            if (scoringContainer != null)
            {
                return scoringContainer;
            }

            DrinkContainer[] containers = FindObjectsByType<DrinkContainer>(FindObjectsSortMode.None);
            foreach (DrinkContainer container in containers)
            {
                if (container.containerType == DrinkContainer.ContainerType.FinalGlass)
                {
                    return container;
                }
            }

            return containers.Length > 0 ? containers[0] : null;
        }

        // ===== 快捷键 C：打印所有容器状态 =====
        // 用于排查量杯、最终杯、后续摇杯等多容器之间的材料记录、容量和风味是否正确。
        private void PrintAllContainerDebugInfo()
        {
            DrinkContainer[] containers = FindObjectsByType<DrinkContainer>(FindObjectsSortMode.None);
            if (containers.Length == 0)
            {
                Debug.LogWarning("No DrinkContainer found in scene.", this);
                return;
            }

            // 按容器显示名排序，保证 Console 输出顺序稳定，方便人工对比。
            Array.Sort(containers, (left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("========== Container Debug ==========");
            foreach (DrinkContainer container in containers)
            {
                builder.AppendLine(container.GetDebugSummary());
            }
            builder.Append("=====================================");

            Debug.Log(builder.ToString(), this);
        }

        // ===== 快捷键 R：清空杯子 =====
        private void ClearCurrentDrink()
        {
            DrinkContainer drinkContainer = FindFirstObjectByType<DrinkContainer>();
            if (drinkContainer == null)
            {
                Debug.Log("No DrinkContainer found in the scene.", this);
                return;
            }

            drinkContainer.Clear();
            Debug.Log("Cup cleared.", drinkContainer);

            if (debugUI != null)
            {
                debugUI.SetVolume(drinkContainer.DisplayName, 0f, drinkContainer.MaxVolume);
                debugUI.SetTasteFeedback("None");
                debugUI.SetScoreResult(null);
            }
        }
    }
}
