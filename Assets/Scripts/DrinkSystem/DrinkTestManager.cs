using CupPrototype.Interaction;
using CupPrototype.Flair;
using CupPrototype.Game;
using CupPrototype.Scoring;
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
        [SerializeField] private DemoRoundManager demoRoundManager;
        // scoringContainer 用于在多容器场景中明确指定按 F 评分的最终杯。
        public DrinkContainer scoringContainer;
        // defaultTransferRatePerSecond 是普通容器之间的持续转移速度。
        public float defaultTransferRatePerSecond = 30f;
        // jiggerTransferRatePerSecond 是 Jigger 作为源容器时的快速转移速度。
        public float jiggerTransferRatePerSecond = 300f;
        // instantJiggerTransfer 勾选后，Jigger 会在点击目标容器时一次性完成转移。
        public bool instantJiggerTransfer = false;
        // 旧字段保留给已有场景数据兼容；内部逻辑优先使用 defaultTransferRatePerSecond。
        [HideInInspector]
        public float containerTransferRatePerSecond = 30f;

        // ===== 当前选择与倒入状态 =====
        private PourableIngredient selectedIngredient;
        private InteractionCoordinator coordinator;
        private PourableIngredient currentSelectedIngredient;
        private DrinkContainer selectedSourceContainer;
        private DrinkContainer currentPourContainer;
        private DrinkContainer currentTransferTargetContainer;
        // 记录当前正在倒入的材料瓶倾斜反馈，避免每帧重复触发 StartTiltTowards。
        private PourTiltFeedback activeIngredientTiltFeedback;
        private bool isPouring;
        private bool isTransferringContainer;
        private bool promptedMissingIngredientThisPress;
        // 当前鼠标按住期间只显示一次非法倒入或转移提示，避免 Update 每帧刷屏。
        private bool hasShownCurrentPourRejection;
        private InputMode currentMode = InputMode.Idle;

        // ===== 给 DragController 查询的全局选择状态 =====
        // 有材料选中时，杯子左键优先用于倒入；没有材料时杯子可以拖动。
        public static bool HasSelectedIngredient { get; private set; }
        // 供外部验证 F 评分当前实际使用的目标引用。
        public TargetDrinkData CurrentTarget => targetDrink;
        public PourableIngredient SelectedIngredient => currentSelectedIngredient;
        public DrinkContainer SelectedSourceContainer => selectedSourceContainer;
        public DrinkContainer ActiveContainer => currentPourContainer != null
            ? currentPourContainer
            : currentTransferTargetContainer != null
                ? currentTransferTargetContainer
                : selectedSourceContainer;
        public string InteractionState => currentMode.ToString();
        public DrinkScoreResult LastEvaluation { get; private set; }

        public event Action SelectionChanged;
        public event Action InteractionStateChanged;
        public event Action<TargetDrinkData> TargetChanged;
        public event Action<DrinkScoreResult> EvaluationChanged;

        // ===== 生命周期：初始化相机和静态状态 =====
        private void Awake()
        {
            coordinator = GetComponent<InteractionCoordinator>();
            HasSelectedIngredient = false;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (demoRoundManager == null)
            {
                demoRoundManager = FindAnyObjectByType<DemoRoundManager>();
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
                hasShownCurrentPourRejection = false;
                ClearCurrentDrink();
            }

            // C 仅用于开发者查看容器内部状态；T、F、R 在两个模式下都保持可用。
            if (DemoModeController.DeveloperModeActive && Input.GetKeyDown(KeyCode.C))
            {
                PrintAllContainerDebugInfo();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                hasShownCurrentPourRejection = false;
                ClearSelectedSourceContainer();
            }
        }

        // ===== 鼠标输入处理 =====
        // 输入优先级：
        // 1. 有材料选中时，按住任何 DrinkContainer 都用于倒入材料；
        // 2. 没有材料选中但有转移源时，按住另一个 DrinkContainer 用于容器转移；
        // 3. 两者都没有时，本脚本不处理拖拽，让 DragController 正常移动物体，ShakerController 再根据移动距离累计摇晃。
        private void HandleMouseInput()
        {
            if (coordinator && coordinator.OwnsHeldInput) return;
            // 花式或模板录制接管鼠标输入时，防止轨迹经过容器误触发倒入、转移或材料选择。
            if (FlairGestureController.IsFlairInputActive ||
                FlairGestureController.IsFlairPlaying ||
                GestureTemplateRecorder.IsTemplateRecording)
            {
                if (isPouring)
                {
                    StopIngredientPour();
                }

                if (isTransferringContainer)
                {
                    StopContainerTransfer();
                }

                promptedMissingIngredientThisPress = false;
                hasShownCurrentPourRejection = false;
                return;
            }

            // 容器转移是 Shift 临时模式；松开 Shift 后立即恢复普通拖动，避免 Shaker 保持为转移源。
            if (selectedSourceContainer != null && !IsShiftPressed())
            {
                ClearSelectedSourceContainer();
            }

            if (Input.GetMouseButtonDown(0))
            {
                promptedMissingIngredientThisPress = false;
                hasShownCurrentPourRejection = false;
                HandleMouseDown();
            }

            if (Input.GetMouseButton(0))
            {
                // 材料倒入优先于容器转移，避免选中材料时误把 Shaker/Jigger 当作转移源。
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
                hasShownCurrentPourRejection = false;
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
                // 已选中转移源时，Shift + 点击另一个容器表示目标，不再把目标错误切换为新的源。
                // Shaker 继续走按住转移；Jigger 保持已有的即时转移配置。
                if (selectedSourceContainer != null && drinkContainer != selectedSourceContainer)
                {
                    if (ShouldInstantTransferFromJigger())
                    {
                        TransferSelectedSourceToTarget(drinkContainer, selectedSourceContainer.CurrentVolume);
                    }

                    return;
                }

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
                // Jigger 瞬间转移：源容器是 Jigger 且开关打开时，点击目标容器就一次性转移。
                if (currentSelectedIngredient == null &&
                    selectedSourceContainer != null &&
                    drinkContainer != selectedSourceContainer &&
                    ShouldInstantTransferFromJigger())
                {
                    TransferSelectedSourceToTarget(drinkContainer, selectedSourceContainer.CurrentVolume);
                    return;
                }

                if (selectedIngredient == null)
                {
                    ShowMessage("No source selected");
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
                StopIngredientPour();
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
            SelectionChanged?.Invoke();
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

            StopContainerTransfer();
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
                ShowMessage("Container is full");
                StopIngredientPour();
                return;
            }

            // 材料瓶直倒 Shaker 必须在 AddIngredient 改变数据前拦截，并停止瓶身倾斜反馈。
            if (!drinkContainer.CanReceiveDirectIngredient(out string rejectionReason))
            {
                ShowPourRejectionOnce($"[DrinkTestManager] {rejectionReason}", rejectionReason, drinkContainer);
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
                ClearEmptiedTransferSource();
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
                ShowMessage("Container is full");
                StopContainerTransfer();
                return;
            }

            if (!isTransferringContainer || currentTransferTargetContainer != targetContainer)
            {
                StartContainerTransfer(targetContainer);
            }

            TransferSelectedSourceToTarget(targetContainer, GetCurrentTransferRatePerSecond() * Time.deltaTime);
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
                ShowMessage("No ingredient selected");
                return;
            }

            if (isPouring && currentPourContainer == targetContainer)
            {
                return;
            }

            currentPourContainer = targetContainer;
            isPouring = true;
            SetInputMode(InputMode.PouringIngredient);

            StartSelectedIngredientTilt(targetContainer);

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

            // 这是视觉反馈，不影响液体转移数据。
            PourTiltFeedback tiltFeedback = selectedSourceContainer.GetComponent<PourTiltFeedback>();
            if (tiltFeedback != null)
            {
                tiltFeedback.StartTiltTowards(targetContainer.transform.position);
            }
        }

        // ===== 容器转移执行入口 =====
        // 持续转移和 Jigger 瞬间转移共用这里，统一处理 UI 更新和源容器变空后的清理。
        private void TransferSelectedSourceToTarget(DrinkContainer targetContainer, float amount)
        {
            if (selectedSourceContainer == null ||
                targetContainer == null ||
                targetContainer == selectedSourceContainer)
            {
                return;
            }

            if (selectedSourceContainer.IsEmpty())
            {
                ClearEmptiedTransferSource();
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            // 持续和瞬间转移共用此入口；非 Jigger 转入 Shaker 时不调用 TransferTo。
            if (!targetContainer.CanReceiveFrom(selectedSourceContainer, out string rejectionReason))
            {
                ShowPourRejectionOnce(
                    $"[DrinkContainer] Transfer rejected: {rejectionReason}",
                    rejectionReason,
                    selectedSourceContainer);
                StopContainerTransfer();
                return;
            }

            bool transferred = selectedSourceContainer.TransferTo(targetContainer, amount);
            if (!transferred)
            {
                ShowMessage("Transfer failed");
            }

            if (transferred && debugUI != null)
            {
                debugUI.SetVolume(targetContainer.DisplayName, targetContainer.CurrentVolume, targetContainer.MaxVolume);
            }

            if (selectedSourceContainer != null && selectedSourceContainer.IsEmpty())
            {
                ClearEmptiedTransferSource();
            }
        }

        // ===== 当前容器转移速度 =====
        // Jigger 作为源容器时使用专用快速速度；其他容器使用普通转移速度。
        private float GetCurrentTransferRatePerSecond()
        {
            if (selectedSourceContainer != null &&
                selectedSourceContainer.containerType == DrinkContainer.ContainerType.Jigger)
            {
                return jiggerTransferRatePerSecond;
            }

            return defaultTransferRatePerSecond;
        }

        // ===== Jigger 瞬间转移判断 =====
        // 只在 Jigger 为源容器且 Inspector 勾选 instantJiggerTransfer 时启用。
        private bool ShouldInstantTransferFromJigger()
        {
            return instantJiggerTransfer &&
                selectedSourceContainer != null &&
                selectedSourceContainer.containerType == DrinkContainer.ContainerType.Jigger;
        }

        // ===== 停止倒入 =====
        private void StopIngredientPour()
        {
            StopActiveIngredientTilt();

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

        // 由 DemoRoundManager 统一更新评分目标，并清除不再属于当前订单的旧反馈。
        public void SetTargetDrink(TargetDrinkData target)
        {
            targetDrink = target;
            ClearPreviousFeedback();
            TargetChanged?.Invoke(targetDrink);
        }

        // 目标切换时复用 R 的完整清空流程，但由调用方统一写入新回合状态。
        public void ResetForTargetSwitch()
        {
            ResetCurrentDrink(false);
        }

        private void StartSelectedIngredientTilt(DrinkContainer targetContainer)
        {
            PourTiltFeedback tiltFeedback = currentSelectedIngredient != null
                ? currentSelectedIngredient.GetComponent<PourTiltFeedback>()
                : null;

            if (tiltFeedback == null || activeIngredientTiltFeedback == tiltFeedback)
            {
                return;
            }

            StopActiveIngredientTilt();
            activeIngredientTiltFeedback = tiltFeedback;

            // 材料瓶倒入时的视觉反馈，不影响实际材料添加、容量或评分。
            activeIngredientTiltFeedback.StartTiltTowards(targetContainer.transform.position);
        }

        private void StopActiveIngredientTilt()
        {
            if (activeIngredientTiltFeedback == null)
            {
                return;
            }

            // 松开鼠标或取消选择时让材料瓶回正。
            activeIngredientTiltFeedback.StopTilt();
            activeIngredientTiltFeedback = null;
        }

        // ===== 停止容器转移 =====
        private void StopContainerTransfer()
        {
            StopSelectedSourceTilt();

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

        // 这是视觉反馈，不影响液体转移数据。
        private void StopSelectedSourceTilt()
        {
            if (selectedSourceContainer == null)
            {
                return;
            }

            PourTiltFeedback tiltFeedback = selectedSourceContainer.GetComponent<PourTiltFeedback>();
            if (tiltFeedback != null)
            {
                tiltFeedback.StopTilt();
            }
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
            ShowMessage("No ingredient selected");
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

            StopIngredientPour();
            selectedIngredient = null;
            currentSelectedIngredient = null;
            UpdateDragBlockState();
            SelectionChanged?.Invoke();
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

            StopContainerTransfer();
            selectedSourceContainer = null;
            UpdateDragBlockState();
            SetInputMode(currentSelectedIngredient != null ? InputMode.IngredientSelected : InputMode.Idle);

            Debug.Log("[DrinkTestManager] Transfer source cleared.", this);
            UpdateTransferSourceUI();
        }

        // ===== 源容器空后自动清除 =====
        // Jigger 或 Shaker 转移完成变空时调用，防止空容器继续占用转移输入状态。
        private void ClearEmptiedTransferSource()
        {
            StopSelectedSourceTilt();
            selectedSourceContainer = null;
            currentTransferTargetContainer = null;
            isTransferringContainer = false;
            UpdateDragBlockState();
            SetInputMode(InputMode.Idle);
            Debug.Log("[DrinkTestManager] Transfer source emptied and cleared.", this);
            UpdateTransferSourceUI();
        }

        // ===== 更新转移源 UI =====
        private void UpdateTransferSourceUI()
        {
            SelectionChanged?.Invoke();

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
            InteractionStateChanged?.Invoke();
            Debug.Log($"[DrinkTestManager] Mode changed: {previousMode} -> {currentMode}", this);
        }

        // ===== 快捷键 T：试味 =====
        private void TasteCurrentDrink()
        {
            if (coordinator && coordinator.isActiveAndEnabled) return;
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
                ShowMessage("No target drink assigned");
                return;
            }

            DrinkContainer drinkContainer = GetScoringContainer();
            if (drinkContainer == null)
            {
                Debug.Log("No DrinkContainer found in the scene.", this);
                ShowMessage("No scoring container found");
                return;
            }

            EvaluateFinishedDrink(drinkContainer, targetDrink);
        }

        public void EvaluateFinishedDrink(DrinkContainer drinkContainer, TargetDrinkData targetDrink)
        {
            if (!drinkContainer || !targetDrink) return;
            Debug.Log($"[DrinkTestManager] Scoring container: {drinkContainer.DisplayName}, MixState={drinkContainer.mixState}, Ingredients: {drinkContainer.GetIngredientDebugString()}", drinkContainer);

            DrinkScoreResult scoreResult = DrinkScoreSystem.ScoreDrink(drinkContainer, targetDrink);
            LastEvaluation = scoreResult;
            EvaluationChanged?.Invoke(LastEvaluation);
            string scoreSummary = DrinkScoreFeedbackFormatter.Format(scoreResult);
            Debug.Log($"[DrinkTestManager] Preparation: {targetDrink.requiredPreparation}, Matched={scoreResult.preparationMatched}, Feedback={scoreResult.preparationFeedback}", drinkContainer);

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
                $"Prep: {scoreResult.preparationFeedback}\n" +
                $"Feedback: {scoreResult.feedbackText}",
                drinkContainer);

            if (debugUI != null)
            {
                debugUI.SetScoreResult(scoreResult);
            }

            if (demoRoundManager != null)
            {
                demoRoundManager.ShowScoreSummary(scoreSummary);
            }
        }

        // 一次鼠标按住只记录和显示一次拒绝；松开、取消、重置或切换目标后重新允许提示。
        private void ShowPourRejectionOnce(string consoleMessage, string panelMessage, UnityEngine.Object context)
        {
            if (hasShownCurrentPourRejection)
            {
                return;
            }

            Debug.Log(consoleMessage, context);
            ShowMessage(panelMessage);
            hasShownCurrentPourRejection = true;
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

        // ===== 快捷键 R：清空所有容器 =====
        // 原型调试时 R 作为全局重置：清空 Cup、Jigger、Shaker 等所有 DrinkContainer，
        // 同时重置输入状态和材料高亮，避免旧选择影响下一轮流程测试。
        private void ClearCurrentDrink()
        {
            ResetCurrentDrink(true);
        }

        // R 与目标切换共用的唯一重置流程，避免复制容器、选择和倒入状态清理逻辑。
        private void ResetCurrentDrink(bool resetRoundState)
        {
            if (coordinator) coordinator.ResetPreparation();
            hasShownCurrentPourRejection = false;
            StopActiveIngredientTilt();
            StopSelectedSourceTilt();

            DrinkContainer[] containers = FindObjectsByType<DrinkContainer>(FindObjectsSortMode.None);
            if (containers.Length == 0)
            {
                Debug.Log("No DrinkContainer found in the scene.", this);
            }

            foreach (DrinkContainer container in containers)
            {
                container.Clear();
            }

            ClearAllSelectionHighlights();
            selectedIngredient = null;
            currentSelectedIngredient = null;
            selectedSourceContainer = null;
            currentPourContainer = null;
            currentTransferTargetContainer = null;
            isPouring = false;
            isTransferringContainer = false;
            promptedMissingIngredientThisPress = false;
            HasSelectedIngredient = false;
            SelectionChanged?.Invoke();
            SetInputMode(InputMode.Idle);

            Debug.Log("[DrinkTestManager] Cleared all containers.", this);
            ClearPreviousFeedback();

            if (debugUI != null)
            {
                debugUI.SetSelectedIngredient("None");
                debugUI.SetTransferSource("None");
                debugUI.SetVolume(0f, 0f);
            }

            // 场景组件可能在 Inspector 绑定之后新增，按 R 时补查并最后写回回合状态。
            if (resetRoundState && demoRoundManager == null)
            {
                demoRoundManager = FindAnyObjectByType<DemoRoundManager>();
            }

            if (resetRoundState && demoRoundManager != null)
            {
                demoRoundManager.ResetRound();
            }
        }

        // 清除上一目标的评分、试味反馈和屏幕错误提示。
        private void ClearPreviousFeedback()
        {
            if (LastEvaluation != null)
            {
                LastEvaluation = null;
                EvaluationChanged?.Invoke(null);
            }

            if (debugUI != null)
            {
                debugUI.SetTasteFeedback("None");
                debugUI.SetScoreResult(null);
            }

            if (DemoMessagePanel.Instance != null)
            {
                DemoMessagePanel.Instance.ClearMessage();
            }
        }

        private void ShowMessage(string message)
        {
            if (DemoMessagePanel.Instance != null)
            {
                DemoMessagePanel.Instance.ShowMessage(message);
            }
        }

        // ===== 清除全部材料高亮 =====
        // 全局重置时使用，确保所有材料瓶的 SelectionHighlight 都恢复未选中。
        private void ClearAllSelectionHighlights()
        {
            SelectionHighlight[] highlights = FindObjectsByType<SelectionHighlight>(FindObjectsSortMode.None);
            foreach (SelectionHighlight highlight in highlights)
            {
                highlight.SetHighlighted(false);
            }
        }
    }
}
