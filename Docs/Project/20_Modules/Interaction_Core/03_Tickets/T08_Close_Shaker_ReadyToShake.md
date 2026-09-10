# T08 — Close Shaker → ReadyToShake

> Status: Functional Closeout Pending — Automated PASS / Manual Pending

## Goal
实现 Shaker 在 `Preparing` 状态下通过固定 `[CLOSE SHAKER]` UI 完成关闭，并进入 `ReadyToShake`。

## Scope
`InteractionCoordinator.cs`、`ShakerPreparation.cs`、必要的 Context UI 脚本、`SampleScene.unity`。

## Do
- 当 Shaker=`Preparing`、`ActionState=Stable` 时，显示固定屏幕按钮 `[CLOSE SHAKER]`。
- 点击按钮后：
  - Coordinator 发起 Close 动作；
  - `ActionState` 进入忙碌状态，期间拒绝冲突输入；
  - Shaker 执行一次关闭表现/动画；
  - 完成后由 Event / Callback 通知 Coordinator；
  - Shaker 状态变为 `ReadyToShake`；
  - `ActionState → Stable`。
- `ReadyToShake` 后 `[CLOSE SHAKER]` 必须隐藏，重复点击不得再次关闭或重复改状态。
- 无 Process Ice 也允许 Close；Process Ice 只是独立制作事实。
- Shaker 内已有液体保持不变，Close 不修改液体量、Ingredient 或 Flavor。
- Bottle / Jigger Held 状态不作为 Close 的必要条件；若存在会冲突的当前 Action，应先拒绝 Close。
- 若当前没有实际开合盖视觉，可采用最小关闭动画/位置变化，但必须让玩家能看懂“Shaker 已关闭”。

## Do Not
- 不做 `[OPEN SHAKER]`。
- 不做 Shake Gesture、Taste、Pour、Remake。
- 不改评分、Flavor、Gesture Recognizer、P5-3 Outline 视觉。

## Acceptance
**Auto:** 编译通过；Preparing 时按钮可用；点击一次后进入 `ReadyToShake`；重复 Close 无效；液体与 ProcessIce 数据保持；T03–T07 回归 PASS。

**Manual:** Preparing 时能看到并点击 `[CLOSE SHAKER]`；关闭表现可理解；完成后按钮隐藏且 Shaker 保持在 Prep Position；原有 Measurement / Transfer / Ice 无新增退化。

## Implementation Check — 2026-09-10

- 固定屏幕 `[CLOSE SHAKER]` 按钮读取 Coordinator.CanCloseShaker；Preparing / Stable 时可用，Busy / Rest / ReadyToShake 时隐藏，点击经 Coordinator 发起关闭。
- 新增明确的 Closing ActionState。关闭期间拦截旧鼠标交互、Held Return、重复 Close、Transfer 及其他 Coordinator 动作；Recipe Terminal 与旧 Flair 输入仅增加 Closing 互斥检查，Recognizer 未修改。
- 复用现有 Boston Shaker Small Tin：Preparing 时将其放到 Large Tin 左侧桌面；关闭时用短暂位置过渡移回原合盖位置。实际完成后设置 ReadyToShake，并回调 Coordinator 恢复 Stable；Shaker 根对象保持 Prep Position。
- Close 不要求 Held 或 Process Ice；不修改液体量、Ingredient、Flavor 或 ProcessIce。双持时 Bottle / Jigger 引用保持原样。
- Reset / 禁用组件时取消关闭过渡，避免稍后错误进入 ReadyToShake；ResetToRest 恢复原 Tin 位置。不实现 OPEN SHAKER 或 Shake Gesture。
- Automated Check：PASS。编译通过；无冰空手合盖、带液体 / ProcessIce 的双持合盖、按钮显隐、忙碌及重复拒绝、完成回调、Tin 最终位置、数据保留及 Reset 取消均通过；T03–T07 回归全部 PASS。日志标记 `T08_AUTOMATED_PASS`。
- 重跑入口：从已保存的 Edit Mode 调用 `InteractionCoordinatorCloseCheck.RunBatch`；批处理运行完自动退出，普通编辑器仅退出 Play Mode。`SetupAndRun` 为首次 Scene 配置入口，不用于重复验收。
- 既有 Renderer / Shader、Flavor、评分、Gesture Recognizer 未修改。当前 Unity AI 服务有账号 / entitlement 日志，自动检查未发现 T08 Gameplay Error / Exception。
- Manual 待确认：Preparing 的按钮位置、Tin 分开放置与合盖表现、合盖后按钮隐藏和 Prep 位置，以及 Measurement / Transfer / Ice 实际操作。尚不关闭 Ticket，不开始下一 Ticket。
