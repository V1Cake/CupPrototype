# T12 — Open Shaker Taste with Stir Stick

> Status: Implemented / Automated Check PASS / Manual Check Pending

## Goal
实现 Open Shaker 状态下的可选 Taste：使用场景中已有的搅拌棒执行一次简化搅拌/尝味动作，并根据 ActiveOrder 给出定性反馈。

## Scope
`InteractionCoordinator.cs`、`CurrentDrinkRecord.cs`、现有 OrderContext / Flavor 只读接口、必要的 Stir Stick/Taste UI 脚本、`SampleScene.unity`。

## Do
- **触发条件**
  - Shaker 必须处于 `Preparing`（即已 Open）。
  - `ActionState=Stable`，Shaker 内存在实际饮品。
  - 玩家点击场景中已有的搅拌棒触发 Taste；不新建新的搅拌棒模型。
  - Closed / ReadyToShake / ShakeComplete 状态下直接点击搅拌棒不得 Taste；应先 Open Shaker。

- **Taste 动作**
  - Coordinator 发起一次短暂 Taste 动作并锁定冲突输入。
  - 复用场景已有搅拌棒，执行最小可理解动画：
    1. 搅拌棒移动到 Shaker；
    2. 在 Shaker 内做一次简短搅拌动作；
    3. 做一个简化“取样/尝味”表现；
    4. 搅拌棒返回原位。
  - 动作完成通过 Event / Callback 收口，`ActionState → Stable`。
  - 本 Ticket 不把搅拌棒纳入长期 Held / 双手系统。

- **Taste 数据与反馈**
  - 使用“当前 Shaker 实际 Flavor”与 `ActiveOrder.ExpectedFlavor` 比较。
  - 优先复用现有评分/配方已有的比较依据；不要另造第二套目标 Flavor。
  - 输出一句定性反馈，只指出最明显的有效偏差，例如：`偏酸`、`甜度不足`、`酒体偏重`；若主要维度均接近目标，可显示 `整体接近预期`。
  - 不显示六维精确数值。
  - Taste 不扣除液体量，不改变 Ingredient / Flavor / ProcessIce / ShakePerformed。
  - `CurrentDrinkRecord.Tasted = true` 表示本次制作至少 Taste 过一次。

- **Taste 后仍可继续修改**
  - Taste 不是锁定点，Shaker 继续保持 `Preparing`。
  - 玩家仍可继续 Jigger Transfer、首次 Process Ice 等合法修改。
  - 若后续实际改变 Shaker 内容，继续沿用 T10 规则使当前 Shake 失效，之后必须 `Close → Shake`。
  - Taste 本身不算内容变更，不会使已完成的 Shake 失效。
  - 同一调配版本只允许 Taste 一次；实际加入非零液体或首次 Process Ice 后版本 +1，才允许再次 Taste。`Tasted` 只表示本杯是否曾 Taste，不单独控制重复 Taste。

- **Reset / Order**
  - R Reset 或切换 ActiveOrder 后，清除 `Tasted` 与 Taste 反馈。
  - Taste 必须读取当前 ActiveOrder，不得读取上一订单或场景中的任意 fallback Container。

## Do Not
- 不实现 Remake。
- 不实现选杯、Serve Ice、Shaker→Cup。
- 不改评分公式、Gesture 阈值、P5-3 Outline。
- 不建立通用 Stir / Spoon Gameplay 系统。

## Acceptance
**Auto:** 编译通过；仅 Open/Preparing Shaker 可 Taste；Taste 使用当前 Shaker + ActiveOrder；不扣液体、不改变 ShakePerformed；Taste 后仍可继续修改；重复 Taste 可重新反馈；R/切单清理；T10/T11 直接依赖回归 PASS。

**Manual:** Open Shaker 后点击现有搅拌棒，能看懂“搅拌→取样→反馈”动作；Closed 状态不能直接 Taste；Taste 后仍能继续加料，若实际修改则需要重新 Close + Shake；无新增 Error。

## Implementation Report — 2026-09-11

### Modified Files
- `Assets/Scripts/Interaction/InteractionCoordinator.cs`
- `Assets/Scripts/Interaction/CurrentDrinkRecord.cs`
- `Assets/Scripts/Interaction/ShakerPreparation.cs`
- `Assets/Scripts/Interaction/StirStickTaste.cs` + `.meta` (new)
- `Assets/Scripts/DrinkSystem/TasteFeedbackSystem.cs`
- `Assets/Scripts/DrinkSystem/DrinkTestManager.cs`
- `Assets/Scripts/UI/ShakerContextUI.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Editor/InteractionCoordinatorTasteCheck.cs` + `.meta` (new)
- `Assets/Editor/InteractionCoordinatorOpenCheck.cs`
- This ticket.

### What Changed
- Click the existing spoon while Stable + Preparing + nonempty Shaker + valid ActiveOrder to begin Tasting. Conflicting inputs remain locked until callback completion.
- Only the seven existing BarSpoon parts move; the shared imported BarTools_Props root, strainers and scoop remain stationary. The spoon dips, stirs, samples and returns. Scene adds a spoon-only click collider, two pose anchors and a qualitative feedback panel.
- Feedback compares the configured Shaker's current flavor with ActiveOrder.ExpectedFlavor using Recipe.flavorTolerance, reporting only the largest deviation beyond tolerance. MVP feedback uses short English text consistent with the current UI.
- CurrentDrinkRecord owns Tasted / TasteFeedback. Reset and order switch cancel the animation and clear both; UI reads record data.
- Taste no longer blocks Open or later modifications. T10's previous Tasted rejection assertion is updated; real ingredient / first Ice changes still invalidate Shake, while Taste does not.
- The legacy T-key arbitrary-container Taste entry is disabled while the Coordinator is active.

### Automated Check — PASS
- Unity compilation and Play Mode T12 passed: spoon raycast, dip/sample/return, unrelated tools stationary, state/order/container gates, Busy/Tab rejection, one qualitative deviation, unchanged liquid/ingredients/ProcessIce/ShakePerformed, repeat Taste, modifications after Taste, re-Shake after actual change, R/order cancellation and feedback clearing.
- Direct dependencies: T10 `InteractionCoordinatorOpenCheck.RunBatch` PASS; T11 `MinimalOrderContextCheck.RunBatch` PASS. No full T03–T10 regression executed.
- Re-run T12 with `InteractionCoordinatorTasteCheck.RunBatch` from a saved SampleScene outside Play Mode. `SetupAndRun` is the one-time scene setup helper, not required for normal checks.
- No runtime Error / Exception during checks. Existing Unity MCP signature/assembly-discovery warnings remain unrelated to Gameplay.
- Existing Renderer / Shader / Outline, Gesture and flavor/scoring formulas remain unchanged.

### Manual Check — Pending
1. Acquire Shaker, transfer liquid while Open, then click the existing spoon. Confirm the stir → sample → return animation is understandable and one feedback sentence appears.
2. Confirm Closed states reject direct Taste; Open again and Taste. Taste itself must preserve completed Shake.
3. Add more liquid after Taste, Taste again, then Close: a new Shake is required. First Ice remains allowed; repeated Ice does not add another fact.
4. During and after Taste, press R or switch order: spoon returns and feedback disappears without stale completion.

### Known Limitations
- Minimal spoon animation and provisional qualitative English feedback; manual visual approval is still required.
- No Remake, cup flow, Serve Ice, generic spoon system or new Gesture behavior. Ticket remains open until Manual Check PASS.
## Confirmed Correction — Taste per Preparation Version
- `PreparationVersion` starts at 0; `LastTastedVersion` starts at -1.
- Successful nonzero Jigger → Shaker addition and first Process Ice increment the version once; successful Taste records that version.
- Same-version Taste is rejected without starting animation or changing feedback/data.
- Open / Close / Shake, empty or failed Transfer and repeated Ice do not advance the version.
- R and order switch reset both version values and Tasted / feedback.
- Manual acceptance: Taste twice (second rejected); add liquid or first Ice then Taste (allowed); empty Transfer / repeated Ice / Open-Close then Taste (still rejected).- Correction verification: Unity compilation, updated T12 version assertions, T10 and T11 direct dependency checks PASS. No runtime Error / Exception; Manual Check remains Pending.

## Confirmed Correction — First Shake Before Taste
- Taste additionally requires at least one successful Shake in this attempt (`HasShaken`). Close alone does not qualify; the Shaker still needs to be Open / Preparing for Taste.
- Content changes clear current `ShakePerformed`, but preserve `HasShaken`; version-gated Taste remains available after modifications.
- R / order switch clear `HasShaken`, requiring a first successful Shake again.
- Verification: updated T12 Play Mode check PASS, including rejection before first Shake, Close-only rejection, successful Shake + Open, post-change version Taste and R/order history reset. No runtime Error / Exception during the completed check. Manual validation pending.
