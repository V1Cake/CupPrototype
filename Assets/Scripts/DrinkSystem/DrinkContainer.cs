using System;
using System.Collections.Generic;
using CupPrototype.Game;
using CupPrototype.Interaction;
using CupPrototype.UI;
using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 饮品容器核心逻辑 =====
    // 只负责容量、颜色、材料记录和风味计算；液体显示交给 LiquidVisualController。
    public class DrinkContainer : MonoBehaviour
    {
        // ===== 容器类型 =====
        // 后续用于区分成品杯、量杯、摇杯、调酒杯等不同工作流。
        public enum ContainerType
        {
            FinalGlass,
            Jigger,
            Shaker,
            MixingGlass
        }

        // ===== 混合状态 =====
        // Shaker 使用该状态标记是否已经摇匀；当前阶段不参与评分，只用于调试和后续玩法扩展。
        public enum MixState
        {
            Unmixed,
            PartiallyMixed,
            Mixed
        }

        // ===== 单个材料加入记录 =====
        [System.Serializable]
        public class IngredientEntry
        {
            public IngredientData ingredient;
            public float amount;
        }

        // ===== 容器身份信息 =====
        // containerName 显示在 Debug UI 和 Console 中；留空时 Awake 会使用 GameObject 名称。
        public string containerName = "Container";
        // containerType 用于区分最终杯、量杯、摇杯等，评分默认优先选择 FinalGlass。
        public ContainerType containerType = ContainerType.FinalGlass;
        // mixState 表示当前容器内容的混合状态；主要给 ShakerController 和 C 键调试使用。
        public MixState mixState = MixState.Unmixed;

        // ===== 容量与颜色状态 =====
        [SerializeField] private float maxVolume = 100f;
        [SerializeField] private bool unlimitedReceive;
        [SerializeField] private float currentVolume;
        [SerializeField] private Color currentColor = Color.clear;

        // ===== 材料记录 =====
        [SerializeField] private List<IngredientEntry> ingredients = new List<IngredientEntry>();

        // ===== 液体视觉控制器 =====
        // 可以手动指定；为空时会从子物体中自动查找。
        public LiquidVisualController liquidVisualController;

        // ===== 转移日志节流 =====
        private const float TransferLogStep = 10f;
        public const string ShakerJiggerOnlyMessage = "Shaker only accepts liquid from Jigger.";
        public const string CupCannotPourMessage = "Cup cannot pour liquid into other containers.";
        public const string JiggerBottleOnlyMessage = "Jigger only accepts liquid directly from bottles.";
        [SerializeField] private bool debugLogs = true;
        private float pendingTransferLogAmount;

        // ===== 对外只读访问 =====
        public float MaxVolume => maxVolume;
        public bool UnlimitedReceive => containerType == ContainerType.Shaker && unlimitedReceive;
        public float CurrentVolume => currentVolume;
        public Color CurrentColor => currentColor;
        public IReadOnlyList<IngredientEntry> Ingredients => ingredients;
        public string DisplayName => string.IsNullOrWhiteSpace(containerName) ? gameObject.name : containerName;

        public event Action<DrinkContainer> StateChanged;

        internal void SetMixState(MixState nextState)
        {
            if (mixState == nextState)
            {
                return;
            }

            mixState = nextState;
            StateChanged?.Invoke(this);
        }

        // ===== 生命周期：绑定液体视觉 =====
        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(containerName) || containerName == "Container")
            {
                containerName = gameObject.name;
            }

            if (liquidVisualController == null)
            {
                liquidVisualController = GetComponentInChildren<LiquidVisualController>(true);
            }

            if (liquidVisualController != null)
            {
                if (debugLogs && DemoModeController.DeveloperModeActive)
                {
                    Debug.Log($"[DrinkContainer] LiquidVisualController linked on {gameObject.name}", this);
                }
                liquidVisualController.Initialize();
                liquidVisualController.UpdateVisual(currentVolume, maxVolume, currentColor);
            }
            else
            {
                Debug.LogWarning($"[DrinkContainer] LiquidVisualController not found on {gameObject.name}", this);
            }
        }

        // ===== 容量判断 =====
        public bool CanAdd(float amount)
        {
            return amount > 0f && currentVolume < maxVolume;
        }

        // ===== 剩余容量查询 =====
        public float GetRemainingVolume()
        {
            return Mathf.Max(0f, maxVolume - currentVolume);
        }

        // ===== 空/满状态查询 =====
        public bool IsEmpty()
        {
            return currentVolume <= 0.001f;
        }

        public bool IsFull()
        {
            return GetRemainingVolume() <= 0.001f;
        }

        // 材料瓶直倒属于非法入口：Shaker 只能通过 Jigger 接收液体。
        // 接收规则放在 DrinkContainer，避免其他输入入口绕过限制。
        public bool CanReceiveDirectIngredient(out string rejectionReason)
        {
            if (containerType == ContainerType.Shaker)
            {
                rejectionReason = ShakerJiggerOnlyMessage;
                return false;
            }

            rejectionReason = string.Empty;
            return true;
        }

        // Jigger 只接收材料瓶直倒；容器转入 Shaker 时只允许 Jigger 来源。
        // Shaker 作为源向 Cup 转出不受影响，Jigger 的直接倒入仍执行原有单材料校验。
        public bool CanReceiveFrom(DrinkContainer sourceContainer, out string rejectionReason)
        {
            if (sourceContainer == null)
            {
                rejectionReason = "Source container is missing.";
                return false;
            }

            // Cup 使用 FinalGlass 类型；成品杯只能接收和评分，不能再作为转移源。
            if (sourceContainer.containerType == ContainerType.FinalGlass)
            {
                rejectionReason = CupCannotPourMessage;
                return false;
            }

            // 所有容器来源都不能倒回 Jigger，只保留 Bottle -> Jigger 的 AddIngredient 入口。
            if (containerType == ContainerType.Jigger)
            {
                rejectionReason = JiggerBottleOnlyMessage;
                return false;
            }

            if (containerType == ContainerType.Shaker && sourceContainer.containerType != ContainerType.Jigger)
            {
                rejectionReason = ShakerJiggerOnlyMessage;
                return false;
            }

            rejectionReason = string.Empty;
            return true;
        }

        // ===== 加入材料 =====
        // 处理容量上限、颜色混合、材料记录，并通知液体视觉更新。
        public void AddIngredient(IngredientData ingredient, float amount, bool logResult = true)
        {
            if (ingredient == null)
            {
                if (logResult)
                {
                    Debug.LogWarning("Cannot add ingredient: IngredientData is null.", this);
                }

                return;
            }

            // 必须在改变容量、颜色、风味或材料记录前拒绝材料瓶直倒 Shaker。
            if (!CanReceiveDirectIngredient(out string rejectionReason))
            {
                if (logResult)
                {
                    Debug.Log($"[DrinkContainer] {rejectionReason}", this);
                }

                ShowMessage(rejectionReason);
                return;
            }

            if (!CanAcceptIngredient(ingredient))
            {
                if (logResult)
                {
                    Debug.Log("[DrinkContainer] Jigger can only hold one ingredient at a time.", this);
                }

                ShowMessage("Jigger cannot mix ingredients");
                return;
            }

            if (!CanAdd(amount))
            {
                if (logResult)
                {
                    Debug.Log($"{DisplayName} is full. Cannot add {ingredient.ingredientName}. Volume: {currentVolume:0.##}/{maxVolume:0.##}", this);
                }

                ShowMessage("Container is full");
                return;
            }

            float addAmount = Mathf.Min(amount, maxVolume - currentVolume);
            AddIngredientInternal(ingredient, addAmount, logResult);
        }

        // ===== 容器内容转移 =====
        // 按源容器现有材料比例，把指定数量转移到目标容器。
        public bool TransferTo(DrinkContainer target, float amount)
        {
            bool unlimited = target != null && target.UnlimitedReceive && containerType == ContainerType.Jigger;
            if (target == null || IsEmpty() || (!unlimited && target.IsFull()) || amount <= 0f)
            {
                return false;
            }

            // 必须在计算和写入任何转移数据前校验来源，防止非 Jigger 绕过 Shaker 入口限制。
            if (!target.CanReceiveFrom(this, out string rejectionReason))
            {
                PourTiltFeedback tiltFeedback = GetComponent<PourTiltFeedback>();
                if (tiltFeedback != null)
                {
                    tiltFeedback.StopTilt();
                }

                Debug.Log($"[DrinkContainer] Transfer rejected: {rejectionReason}", this);
                ShowMessage(rejectionReason);
                return false;
            }

            if (target == this)
            {
                return false;
            }

            float sourceVolumeBefore = currentVolume;
            float actualAmount = unlimited ? Mathf.Min(amount, currentVolume)
                : Mathf.Min(amount, currentVolume, target.GetRemainingVolume());
            MixState sourceMixState = mixState;
            if (actualAmount <= 0f)
            {
                return false;
            }

            List<IngredientEntry> transferEntries = new List<IngredientEntry>();
            // 先复制出本次要转移的材料明细，避免遍历 ingredients 时直接修改同一个集合。
            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == null || entry.amount <= 0f)
                {
                    continue;
                }

                float transferAmount = actualAmount * (entry.amount / sourceVolumeBefore);
                if (transferAmount > 0f)
                {
                    transferEntries.Add(new IngredientEntry
                    {
                        ingredient = entry.ingredient,
                        amount = transferAmount
                    });
                }
            }

            if (transferEntries.Count == 0)
            {
                return false;
            }

            // Jigger 是控量工具，不是混合容器；目标为 Jigger 时，只允许转入一种材料，
            // 且该材料必须和 Jigger 当前已有材料一致，或 Jigger 为空。
            if (target.containerType == ContainerType.Jigger &&
                (!TryGetSingleIngredient(transferEntries, out IngredientData singleTransferIngredient) ||
                !target.CanAcceptIngredient(singleTransferIngredient)))
            {
                Debug.Log("[DrinkContainer] Cannot transfer mixed or different ingredient content into Jigger.", target);
                ShowMessage("Jigger cannot mix ingredients");
                return false;
            }

            // 按比例把材料记录转移到目标容器，再从源容器扣除对应数量。
            foreach (IngredientEntry transferEntry in transferEntries)
            {
                target.AddIngredientInternal(transferEntry.ingredient, transferEntry.amount, false, false);
                SubtractIngredientAmount(transferEntry.ingredient, transferEntry.amount);
            }

            // 目标是摇杯时，新进入的内容默认视为尚未摇匀。
            if (target.containerType == ContainerType.Shaker)
            {
                target.mixState = MixState.Unmixed;
                ShakerController targetShakerController = target.GetComponent<ShakerController>();
                if (targetShakerController != null)
                {
                    targetShakerController.ResetShake();
                }
            }
            else
            {
                // Shaker 转移到 Cup/Jigger 时保留“是否摇匀”的工艺状态，方便后续调试；
                // 当前 MixState 只做流程标记，不参与 DrinkScoreSystem 评分。
                target.mixState = sourceMixState;
            }

            RemoveEmptyIngredientEntries();
            RecalculateFromIngredients();
            if (containerType == ContainerType.Shaker && IsEmpty())
            {
                ShakerController sourceShakerController = GetComponent<ShakerController>();
                if (sourceShakerController != null)
                {
                    sourceShakerController.ResetShake();
                }
                else
                {
                    mixState = MixState.Unmixed;
                }
            }

            UpdateLiquidVisual();
            StateChanged?.Invoke(this);
            target.StateChanged?.Invoke(target);

            pendingTransferLogAmount += actualAmount;
            if (debugLogs && DemoModeController.DeveloperModeActive &&
                (pendingTransferLogAmount >= TransferLogStep || IsEmpty()))
            {
                Debug.Log($"[DrinkContainer] Transfer {pendingTransferLogAmount:0.##} from {DisplayName} to {target.DisplayName}", this);
                Debug.Log($"[DrinkContainer] Source {DisplayName} ingredients: {GetIngredientDebugString()}", this);
                Debug.Log($"[DrinkContainer] Target {target.DisplayName} ingredients: {target.GetIngredientDebugString()}", target);
                pendingTransferLogAmount = 0f;
            }

            return true;
        }

        // ===== 清空杯子 =====
        // 只清空 DrinkContainer 自身数据，并通知视觉控制器隐藏液体。
        public void Clear()
        {
            currentVolume = 0f;
            currentColor = Color.clear;
            mixState = MixState.Unmixed;
            ingredients.Clear();
            if (liquidVisualController != null)
            {
                liquidVisualController.ClearVisual();
            }

            ShakerController shakerController = GetComponent<ShakerController>();
            if (shakerController != null)
            {
                shakerController.ResetShake();
            }

            StateChanged?.Invoke(this);
        }

        // ===== 当前综合风味 =====
        // 根据所有已加入材料和数量计算加权平均风味。
        public FlavorProfile GetCurrentFlavorProfile()
        {
            if (currentVolume <= 0f)
            {
                return FlavorProfile.Empty();
            }

            FlavorProfile profile = FlavorProfile.Empty();
            float totalAmount = 0f;

            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == null || entry.amount <= 0f)
                {
                    continue;
                }

                profile.AddWeighted(entry.ingredient, entry.amount);
                totalAmount += entry.amount;
            }

            profile.Divide(totalAmount);
            return profile;
        }

        // ===== Jigger 材料接收判断 =====
        // Jigger 是控量工具，不是混合容器：空的时候可接收任意一种材料；
        // 非空时只能继续接收同一种 IngredientData，不能混入其他材料。
        public bool CanAcceptIngredient(IngredientData ingredient)
        {
            if (ingredient == null)
            {
                return false;
            }

            if (containerType != ContainerType.Jigger)
            {
                return true;
            }

            if (currentVolume <= 0.001f || ingredients.Count == 0)
            {
                return true;
            }

            IngredientData existingIngredient = null;
            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == null || entry.amount <= 0.001f)
                {
                    continue;
                }

                if (existingIngredient == null)
                {
                    existingIngredient = entry.ingredient;
                    continue;
                }

                if (existingIngredient != entry.ingredient)
                {
                    return false;
                }
            }

            return existingIngredient == null || existingIngredient == ingredient;
        }

        // ===== 材料查询 =====
        // 评分系统用它检查关键材料是否出现过。
        public bool ContainsIngredient(IngredientData ingredient)
        {
            if (ingredient == null)
            {
                return false;
            }

            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == ingredient && entry.amount > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        // ===== 材料记录只读访问 =====
        public IReadOnlyList<IngredientEntry> GetIngredientEntries()
        {
            return ingredients;
        }

        // ===== 材料记录调试字符串 =====
        // Console 调试和评分检查时使用，用来确认容器里到底有哪些 IngredientData。
        public string GetIngredientDebugString()
        {
            if (ingredients.Count == 0)
            {
                return "Empty";
            }

            List<string> parts = new List<string>();
            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == null || entry.amount <= 0.001f)
                {
                    continue;
                }

                string ingredientName = !string.IsNullOrWhiteSpace(entry.ingredient.ingredientName)
                    ? entry.ingredient.ingredientName
                    : entry.ingredient.name;

                parts.Add($"{ingredientName}: {entry.amount:0.0}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "Empty";
        }

        // ===== 风味调试字符串 =====
        // Console 调试时使用，把当前加权风味压缩成一行，方便和目标饮品对照。
        public string GetFlavorDebugString()
        {
            FlavorProfile profile = GetCurrentFlavorProfile();
            return $"Sour={profile.sourness:0.0}, Sweet={profile.sweetness:0.0}, Bitter={profile.bitterness:0.0}, Fresh={profile.freshness:0.0}, Body={profile.body:0.0}, Aroma={profile.aroma:0.0}";
        }

        // ===== 容器完整调试摘要 =====
        // C 键批量检查所有容器时使用，集中输出身份、容量、材料、风味和颜色。
        public string GetDebugSummary()
        {
            string debugName = string.IsNullOrWhiteSpace(containerName) ? gameObject.name : containerName;
            string shakerInfo = TryGetComponent(out ShakerController shakerController)
                ? $", {shakerController.GetShakeDebugString()}"
                : string.Empty;

            return $"[{debugName}] Type={containerType}, Volume={currentVolume:0.0}/{maxVolume:0.0}, MixState={mixState}{shakerInfo}, Ingredients={{{GetIngredientDebugString()}}}, Flavor={{{GetFlavorDebugString()}}}, Color=RGBA({currentColor.r:0.00},{currentColor.g:0.00},{currentColor.b:0.00},{currentColor.a:0.00})";
        }

        // ===== 内部工具：不经过容量判断的加料入口 =====
        // 公开 AddIngredient 负责容量裁剪；TransferTo 已经提前计算好可转移量。
        private void AddIngredientInternal(IngredientData ingredient, float amount, bool log, bool notifyStateChanged = true)
        {
            if (ingredient == null || amount <= 0f)
            {
                return;
            }

            if (!CanAcceptIngredient(ingredient))
            {
                if (log)
                {
                    Debug.Log("[DrinkContainer] Jigger can only hold one ingredient at a time.", this);
                }

                ShowMessage("Jigger cannot mix ingredients");
                return;
            }

            AddOrUpdateIngredientEntry(ingredient, amount);
            if (containerType == ContainerType.Shaker)
            {
                mixState = MixState.Unmixed;
                ShakerController shakerController = GetComponent<ShakerController>();
                if (shakerController != null)
                {
                    shakerController.ResetShake();
                }
            }

            RecalculateFromIngredients();
            UpdateLiquidVisual();

            if (notifyStateChanged)
            {
                StateChanged?.Invoke(this);
            }

            if (log && debugLogs && DemoModeController.DeveloperModeActive)
            {
                Debug.Log($"Added ingredient to {DisplayName}: {ingredient.ingredientName}, Amount: {amount:0.##}, Volume: {currentVolume:0.##}/{maxVolume:0.##}, Color: R={currentColor.r:0.00}, G={currentColor.g:0.00}, B={currentColor.b:0.00}, A={currentColor.a:0.00}", this);
                Debug.Log($"[DrinkContainer] {DisplayName} ingredients: {GetIngredientDebugString()}", this);
            }
        }

        // ===== 内部工具：刷新液体视觉 =====
        private void UpdateLiquidVisual()
        {
            if (liquidVisualController != null)
            {
                liquidVisualController.UpdateVisual(currentVolume, maxVolume, currentColor);
            }
            else
            {
                Debug.LogWarning($"[DrinkContainer] LiquidVisualController not found on {gameObject.name}", this);
            }
        }

        // ===== 内部工具：新增或累计材料记录 =====
        private void AddOrUpdateIngredientEntry(IngredientData ingredient, float amount)
        {
            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == ingredient)
                {
                    entry.amount += amount;
                    return;
                }
            }

            ingredients.Add(new IngredientEntry
            {
                ingredient = ingredient,
                amount = amount
            });
        }

        // ===== 内部工具：判断一组材料记录是否只包含一种材料 =====
        // Jigger 接收容器转入时使用，防止把混合液倒进控量杯。
        private static bool TryGetSingleIngredient(List<IngredientEntry> entries, out IngredientData ingredient)
        {
            ingredient = null;

            foreach (IngredientEntry entry in entries)
            {
                if (entry.ingredient == null || entry.amount <= 0.001f)
                {
                    continue;
                }

                if (ingredient == null)
                {
                    ingredient = entry.ingredient;
                    continue;
                }

                if (ingredient != entry.ingredient)
                {
                    return false;
                }
            }

            return ingredient != null;
        }

        // ===== 内部工具：从源容器扣除材料数量 =====
        private void SubtractIngredientAmount(IngredientData ingredient, float amount)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i].ingredient == ingredient)
                {
                    ingredients[i].amount -= amount;
                    return;
                }
            }
        }

        // ===== 内部工具：移除接近 0 的材料记录 =====
        private void RemoveEmptyIngredientEntries()
        {
            for (int i = ingredients.Count - 1; i >= 0; i--)
            {
                if (ingredients[i].ingredient == null || ingredients[i].amount <= 0.001f)
                {
                    ingredients.RemoveAt(i);
                }
            }
        }

        // ===== 内部工具：根据材料记录重算容量和颜色 =====
        private void RecalculateFromIngredients()
        {
            currentVolume = 0f;
            currentColor = Color.clear;

            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == null || entry.amount <= 0f)
                {
                    continue;
                }

                float previousVolume = currentVolume;
                currentVolume += entry.amount;
                currentColor = previousVolume <= 0f
                    ? entry.ingredient.displayColor
                    : Color.Lerp(currentColor, entry.ingredient.displayColor, entry.amount / currentVolume);
            }
        }

        private static void ShowMessage(string message)
        {
            if (DemoMessagePanel.Instance != null)
            {
                DemoMessagePanel.Instance.ShowMessage(message);
            }
        }
    }
}
