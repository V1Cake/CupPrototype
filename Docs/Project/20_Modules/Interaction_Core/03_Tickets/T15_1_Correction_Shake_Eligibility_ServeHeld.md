# T15 Correction — Shake Eligibility + Post-Shake Serve Held

> Status: Implemented / Automated PASS / Manual Pending  
> Purpose: 修正 Shake 合法性，并让 Shake 成功后自然进入出杯持壶状态。  
> Note: T15 暂不 Close，完成本修正后一起做 Manual Check。

## Goal
1. 只有具备最低调配内容的 Shaker 才允许 Shake。  
2. Shake 成功后，Shaker 直接进入 Serve Held，玩家可直接点击 Serve Cup 出杯。

## Scope
`InteractionCoordinator.cs`、`ShakerPreparation.cs`、必要的提示 UI / 检查脚本；尽量不改 Scene。

## Do

- **Shake 合法性**
  - 只有 Shaker 内存在 **至少 2 种不同液体 Ingredient** 时，才允许进入成功 Shake 流程。
  - 以下情况禁止 Shake：Shaker 为空；只有 Process Ice；只有 1 种液体；1 种液体 + Process Ice。
  - 不检查是否为正确 Recipe，也不检查比例；玩家仍可自由乱配。
  - 合法性判断必须读取当前 Shaker 的实际 Ingredient 数据，不新增第二套计数状态。

- **非法 Shake 反馈**
  - 玩家在 `ReadyToShake` 状态下从 Shaker 上做 Shake Gesture，但内容不满足最低条件时：
    - 不进入 Shake 动画；
    - `ShakePerformed` 保持 false；
    - Shaker 保持 `ReadyToShake`；
    - 显示一句短提示，例如 `原料不足，暂时无法摇制`；
    - 提示约 1.5–2 秒后自动消失。
  - 重复非法尝试可重新显示提示，但不得累积多个 UI 实例。
  - R / 切单时提示立即清除。

- **Shake 成功后的 Serve Held**
  - 合法 Shake 成功并完成动画后：
    - Shaker 状态 = `ShakeComplete`
    - `ShakePerformed = true`
    - Shaker 自动进入出杯用的 **Serve Held** 状态 / Pose。
  - Serve Held 仅用于出杯，不进入 `Preparing`。
  - 若已有 Serve Cup：玩家直接点击当前 Cup → 进入现有 T15 AutoPour，不需要再点击一次 Shaker。
  - 若尚未选杯：Shaker 保持 Serve Held，玩家可先选杯 / Serve Ice，再点击 Cup 出杯。
  - RMB：Shaker 回 Prep Position，保持 `ShakeComplete` 与 `ShakePerformed=true`。
  - 放回后仍可按现有 T15 逻辑重新点击 Shaker进入出杯准备。
  - 主动 `[OPEN SHAKER]` 仍需先离开 Serve Held / 回 Prep，再按 T10 规则进入 `Preparing`。

- **T15 对接**
  - 保留现有 AutoPour、容量裁剪、Waste 清理、Shaker Auto Return。
  - 兼容两种入口：
    1. Shake 成功后 Serve Held → 直接点击 Cup；
    2. Shaker 已放回 Prep → 点击 Shaker → 点击 Cup。
  - 不允许两个入口重复触发同一次 Pour。

## Do Not
- 不根据 Recipe 正确性决定能不能 Shake。
- 不修改 Gesture 阈值 / 模板。
- 不修改 Flavor、评分、Outline / Shader。
- 不实现 Submit / Waste UI。

## Acceptance
**Auto:** 空 / 仅冰 / 单一液体均拒绝 Shake；两种及以上液体可正常 Shake；非法尝试只显示短提示且状态不前进；Shake 成功后进入 Serve Held；直接点击 Cup 可触发 T15 AutoPour；RMB 可回 Prep 且仍保持 ShakeComplete；T15 直接依赖检查 PASS。

**Manual:** 单液体 Shake 时提示清楚且自动消失；两种液体可正常 Shake；Shake 后 Shaker 明显处于手持出杯位置，选杯后可直接点 Cup 倒酒；RMB 放回后仍可正常继续出杯；无新增 Error。

## Implementation
- ShakerPreparation.HasShakeIngredients reads positive-amount Ingredient entries directly, requiring two distinct Ingredient assets. Both gesture entry and Shake execution validate this condition; Recipe correctness and proportions are not checked.
- Existing DemoMessagePanel displays `Not enough ingredients to shake.` with its existing two-second expiry. Repeated attempts reuse the same panel; ResetPreparation clears it for R / order changes.
- Successful Shake ends in the existing Serve Hold pose with Shaker as PrimaryHeld, even without a selected Cup. Clicking another Rack Cup selects it; clicking the current Serve Cup starts AutoPour.
- RMB returns to Prep without clearing ShakePerformed. OPEN is unavailable while Serve Held; return first, then use the existing Reopen flow.
- T15 AutoPour, capacity, Waste and return logic are unchanged. No Scene or UI prefab changes.

## Modified Files
- Assets/Scripts/Interaction/InteractionCoordinator.cs
- Assets/Scripts/Interaction/ShakerPreparation.cs
- Assets/Editor/InteractionAutoPourCheck.cs
- This ticket and T15 check/flow notes.

## Automated Check
- Run: InteractionAutoPourCheck.RunBatch (extended T15.1 + T15 coverage).
- PASS (2026-09-12): compilation; empty / ice-only / single liquid / single liquid + ice rejection; repeated feedback reuses one panel; timed expiry and immediate R/order clearing; two actual Ingredient assets permit Shake; automatic Serve Held without Cup; selecting Cup and Serve Ice while Held; direct Pour and RMB/reacquire Pour; duplicate Pour rejection; existing capacity/Waste/facts/reset checks.
- Log: `%TEMP%/CupPrototype-T15-1.log`, marker `T15_1_AUTOMATED_PASS + T15_AUTOMATED_PASS`.
- No runtime Error / Exception in the check. Current scene logs `LiquidVisualController not found on CoupeGlass_01_Prop`; liquid visual configuration was not changed by this correction. Existing Unity obsolete API warnings remain.
- Manual Check remains Pending; T15 is not Closed. No full Interaction Core regression run.
