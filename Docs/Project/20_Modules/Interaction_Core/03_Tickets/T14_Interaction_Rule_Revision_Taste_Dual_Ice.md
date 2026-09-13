# Interaction Rule Revision — Taste + Dual Ice Sources

> Status: Implemented / Automated PASS / Manual Pending  
> Purpose: 修正 Taste 顺序，并将 Process Ice / Serve Ice 拆成两个独立物理冰源。  
> Scope: 只修改现有 Interaction 规则与必要场景对象，不推进后续 Pour / Submit。

## Goal

完成两项规则修正：

1. Taste 不再要求先 Shake；只要 Shaker 处于 Open / Preparing 且已有液体，就允许 Taste。
2. Process Ice 与 Serve Ice 使用两个独立冰槽，玩家通过点击不同冰槽明确决定冰加到哪里。

## Do

### A. Taste Rule Revision

- Taste 的合法条件改为：
  - Shaker = `Preparing`
  - Shaker 内存在液体
  - `ActionState = Stable`
  - 当前配方版本尚未 Taste
- 不要求 `ShakePerformed = true`，也不要求先进入 `ShakeComplete`。
- 玩家首次加入部分或全部原料后，就可以用现有 Stir Stick Taste。
- Reopen 后只要实际修改了配方，仍可再次 Taste；沿用现有 `PreparationRevision / LastTastedRevision` 规则。
- Taste 反馈改为两级判断：
  1. **先检查当前 ActiveOrder 的 Recipe 完整性**
     - 若订单要求的某个必要原料当前完全缺失，优先提示“似乎还缺了点东西”或等价定性反馈。
     - 不显示具体缺少多少 ml。
  2. **若主要原料均已出现，再比较当前 Shaker Flavor 与 ActiveOrder.ExpectedFlavor**
     - 保留“偏酸 / 甜度不足 / 酒体偏重 / 整体接近预期”等定性反馈。
- Taste 仍然：
  - 不扣液体
  - 不修改 Flavor / Ingredient
  - 不修改 ProcessIce / ServeIce
  - 不直接改变 Shake 状态
- Closed / ReadyToShake / ShakeComplete 状态下点击 Stir Stick 不直接 Taste；需要先主动 Open 回 `Preparing`。

### B. Dual Ice Sources

- 将当前单一 Ice Well 拆成两个明确交互源：
  - `Shake Ice Well`
  - `Serve Ice Well`
- `Shake Ice Well`
  - 只作用于 `Preparing` Shaker
  - 只写入 `ProcessIce`
  - 仍遵守单次制作只添加一次 Process Ice
- `Serve Ice Well`
  - 只作用于当前位于 ServePosition 的空 Cup
  - 只写入 `ServeIce`
  - 同一当前杯只允许一次 Serve Ice
- 两个冰槽互不复用状态，不再通过 Shaker 状态猜测“这次冰应该加到哪里”。
- 若目标条件不满足，点击对应冰槽应拒绝且不改变数据。
- 场景中允许复用现有冰槽模型并复制一个实例，通过位置 / 标签 / 简单视觉差异让玩家能区分用途；不要因此重做完整环境美术。
- 若复制冰槽后出现遮挡或 Raycast 冲突，只做最小 Transform 调整；不要改 Camera 或大改 Collider。

## Do Not

- 不实现冰型 / 冰量选择。
- 不实现 Shaker → Cup Pour、Submit、Service Zone。
- 不修改评分公式、Flavor 算法、Gesture 阈值、Outline / Shader。
- 不重做完整 Workstation Layout。

## Acceptance

**Auto**
- Taste 在 `Preparing + 有液体` 时可触发，不再依赖 ShakeComplete。
- 缺少必要原料时优先给“缺东西”类提示；原料基本齐全后才输出 Flavor 偏差。
- 同版本重复 Taste 仍被拒绝，实际修改后重新允许。
- Shake Ice 只改变 `ProcessIce`；Serve Ice 只改变 `ServeIce`。
- 两个冰槽目标错误时均不改数据。
- T12 / T13 直接依赖回归 PASS。

**Manual**
- 加入部分原料后即可 Taste，动作与反馈正常。
- 漏掉原料时能看到“缺东西”类提示；补齐后再次 Taste 可得到 Flavor 偏差反馈。
- 两个冰槽在 Game View 中容易区分、容易点击。
- Shake Ice 明确加到 Shaker，Serve Ice 明确加到当前杯。
- Bottle / Jigger / Shaker / Cup 既有点击不受影响。

## Confirmed Follow-up Rules
- Cup selection requires Stable, not a particular Shaker state. Open, Shake and Taste are not prerequisites.
- Serve Ice requires the current Serve Cup, no liquid (CurrentVolume <= 0), and ServeIce not yet set, while interaction is Stable. It never depends on Shaker state.
- Empty iced Cup replacement clears old ServeIce before returning the Cup; new Cup starts with ServeIce=false and must be iced explicitly. ProcessIce is unaffected. A Cup with positive liquid volume cannot be replaced.

## Implementation
- CurrentDrinkRecord owns independent ProcessIce / ServeIce. SelectGlass changes and attempt reset clear ServeIce. Serve Ice does not alter preparation version, Taste or Shake facts.
- InteractionCoordinator routes each source explicitly. Existing iceWell reference is the Shake Ice Well; separate serveIceWell acts only on the current Cup.
- Existing well duplicated for Serve Ice. Separate world labels identify SHAKE ICE / SERVE ICE and display ADDED from the record. Cups have no ice models in this MVP; there is no retained visual ice on returned cups.
- Taste uses the current Shaker and ActiveOrder.requiredIngredients first, reusing ContainsIngredient; if complete, the existing Flavor deviation formatter is used. No Shake prerequisite and no changes to flavor/scoring formulas.
- T12 test updated for pre-Shake Taste and missing-ingredient priority. T13 test updated for Cup selection in Rest / Preparing / Ready and ShakeComplete. T14 check covers both physical sources, target rejection, Busy/repeat rejection, independent data, swap clearing, nonempty rejection and Reset/order cleanup.

## Modified Files
- Assets/Scripts/Interaction/CurrentDrinkRecord.cs
- Assets/Scripts/Interaction/InteractionCoordinator.cs
- Assets/Scripts/DrinkSystem/TasteFeedbackSystem.cs
- Assets/Scripts/UI/ShakerContextUI.cs
- Assets/Scenes/SampleScene.unity
- Assets/Editor/InteractionDualIceCheck.cs + .meta
- Assets/Editor/InteractionCoordinatorTasteCheck.cs
- Assets/Editor/InteractionCoordinatorCupCheck.cs
- T12, T13 and this ticket.

## Automated Check — PASS (2026-09-12)
- Unity compilation and T14 Play Mode checks passed with no test runtime Error / Exception. T12 and T13 direct regressions passed; no full Interaction Core regression run.
- Verified independent ice facts, repeated/wrong/busy target rejection, empty iced Cup replacement clearing only ServeIce, positive-volume swap rejection, pre-Shake Taste and R/order reset.
- Shake Ice Well world position: (-0.43, 0.91, 1.01); Serve Ice Well: (-0.64, 0.91, 0.68). Minimal well placement avoids the Serve Cup blocking the Shake Ice target. Existing collider shapes and Camera are unchanged.
- Labels follow record data. Scene reload verifies label positions; rendered Camera inspection confirms both labels are visible. Label anchors are 0.20 above their respective wells.
- Manual interaction/visual approval remains pending; this ticket is not Closed.

## Manual Check — Pending
1. Add part of an order's required ingredients to Preparing Shaker; Taste before Shake gives the missing-content hint.
2. Add the missing ingredients and Taste again; receive qualitative Flavor feedback. Same-version repeats remain rejected.
3. Select a Cup before any Shake; Serve Ice works only on the empty current Cup. Shake Ice works only on Preparing Shaker.
4. Ice an empty Cup, replace it: Serve label returns to uniced and the new Cup needs a fresh Serve Ice click. Process Ice remains set.
5. Confirm both wells are legible and clickable, and Bottle/Jigger/Shaker/Cup interactions remain usable.

No Pour, Submit, ice amount/type system or general workstation redesign.
