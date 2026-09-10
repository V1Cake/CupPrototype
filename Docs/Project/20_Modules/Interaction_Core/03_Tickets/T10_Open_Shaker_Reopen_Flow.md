# T10 — Open Shaker / Reopen Flow

> Status: Implemented / Automated Check PASS / Manual Check Pending

## Goal

实现 `[OPEN SHAKER]`，支持 Taste 前从 `ReadyToShake` 或 `ShakeComplete` 重新打开 Shaker 并回到 `Preparing`。

## Scope

`InteractionCoordinator.cs`、`ShakerPreparation.cs`、`ShakerContextUI.cs`、`CurrentDrinkRecord.cs`、必要的 `SampleScene.unity` 修改。

## Do

- 当 Shaker=`ReadyToShake` 或 `ShakeComplete`、`ActionState=Stable` 且当前饮品尚未 Taste 时，显示固定 `[OPEN SHAKER]`。
- 点击后执行一次 Open 动作，并由 Event / Callback 收口：
  - `ReadyToShake → Preparing`
  - `ShakeComplete → Preparing`
  - Shaker 保持在 Prep Position
  - 液体内容、Ingredient、ProcessIce 全部保留
  - 最终 `ActionState → Stable`
- 若从 `ShakeComplete` 打开：
  - `CurrentDrinkRecord.ShakePerformed = false`
  - 后续必须重新 `[CLOSE SHAKER] → ReadyToShake → Shake Gesture`，才能再次成为 `ShakeComplete`。
- 打开后 `[OPEN SHAKER]` 隐藏，现有 `[CLOSE SHAKER]` 重新出现；玩家可继续 Jigger → Shaker Transfer。
- R Reset 后按钮与 Shaker 状态必须正确恢复，不残留 Open/Close UI。
- Busy 状态下拒绝 Open，不改变任何 Gameplay 数据。

## Do Not

- 不实现 Taste 按钮、Taste 文案或 Remake。
- 不修改液体、Flavor、评分、Gesture Recognizer。
- 不修改 P5-3 Outline 视觉。

## Acceptance

**Auto:** 编译通过；ReadyToShake/ShakeComplete 均可 Open→Preparing；ShakeComplete Open 会清除 `ShakePerformed`；液体与 ProcessIce 保留；Tasted/Busy 时拒绝；T03–T09 回归 PASS。

**Manual:** Close 后可重新 Open 并继续加料；ShakeComplete 后 Open→重新 Close→重新 Shake 流程正常；按钮显示/隐藏正确，无状态残留或新增 Error。

## Implementation Check — 2026-09-10

- SampleScene 新增固定 `[OPEN SHAKER]`，复用 Close 按钮的样式和位置；ShakerContextUI 读取 Coordinator 的合法性条件，不自行保存阶段状态。
- ReadyToShake / ShakeComplete、Stable、尚未 Tasted 时显示 Open。点击进入 Opening，Small Tin 用 0.35 秒回到已有开盖位置；实际动画完成回调恢复 Stable，Shaker 进入 Preparing 并留在 Prep，Open 隐藏、Close 显示。
- 从 ShakeComplete 开始 Open 时立即清除 ShakePerformed；已有液体、Ingredient、ProcessIce 和 Bottle / Jigger Held 不变。再次 Close 只回 ReadyToShake，必须再次成功 Shake 才能重新记录完成。
- CurrentDrinkRecord 增加 Tasted 事实和 Shake 清除入口，R 的既有 ResetCurrentDrink 路径同步清除事实、取消 Opening 并回 Rest。未实现 Taste 按钮、反馈或 Remake。
- Opening 期间复用 Coordinator 动作锁拒绝重复 Open、Close、Return、Transfer 等冲突操作。Reset / 禁用时取消开盖，不允许迟到回调恢复 Preparing 或旧 Shake 完成状态。
- Automated Check：PASS。当前 Unity 编辑器编译通过；实际按钮触发的两种 Open、可见 Tin 位移、完成回调、Prep、液体及 Ice 保留、双持引用保留、再次 Transfer / Close / Shake、Taste / Busy 拒绝、开盖中 R 清理事实和按钮均通过。Taste 拒绝使用测试注入的 Tasted 制作事实，不代表已实现 Taste Gameplay。
- T03–T09 回归：PASS。复用 InteractionCoordinatorCloseCheck（T03–T08）和 InteractionCoordinatorShakeCheck（T09）。日志标记：`T10_AUTOMATED_PASS`、`T09_AUTOMATED_PASS`、`T08_AUTOMATED_PASS`；本轮日志副本为 `%TEMP%/CupPrototype-T10-validation.log`。
- 重跑入口：已保存的 Edit Mode 下调用 `InteractionCoordinatorOpenCheck.RunBatch`。`SetupAndRun` 为 Scene 按钮配置入口。普通编辑器检查完成后仅退出 Play Mode，不关闭编辑器。
- 未修改 DrinkSystem、Flavor、评分、Gesture 采样 / Recognizer / 模板 / 阈值、P5-3 Renderer / Shader。未发现新增 Gameplay Error / Exception；既有 Unity entitlement / 服务及旧 API 警告仍存在。
- Manual Check 待确认：Close → Open → 继续加料；ShakeComplete → Open → Close → 再次 Shake；开盖中按 R 后按钮隐藏、Shaker 回 Rest，重新开始一杯后按钮正常。当前 Ticket 尚未 Closed，不开始下一 Ticket。
