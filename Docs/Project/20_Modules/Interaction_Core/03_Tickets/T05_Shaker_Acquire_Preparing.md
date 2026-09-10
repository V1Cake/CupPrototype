# T05 — Shaker Acquire → Preparing

> Status: Functional Closeout Pending — Automated PASS / Manual Pending

## Goal
建立 Shaker 的正式生命周期起点：`Rest → Normal Acquire → Preparing`，为下一票 Jigger → Shaker AutoTransfer 提供合法目标状态。

## Scope
`InteractionCoordinator.cs`、必要的 Shaker 状态脚本、`SampleScene.unity`。

## Do
- Shaker 自己保存最小状态：`Rest / Preparing / ReadyToShake / ShakeComplete`；本 Ticket 只实际使用 `Rest / Preparing`。
- 当 Shaker 处于 `Rest` 且 `ActionState = Stable` 时，玩家点击 Shaker：
  - Coordinator 发起 Normal Acquire；
  - Shaker 自动从 Rest Position 移动到固定 `ShakerPrepPosition`；
  - 动作完成后状态变为 `Preparing`；
  - Shaker 不进入长期 `PrimaryHeld`。
- `Preparing` 状态再次点击 Shaker不重复 Acquire、不产生第二个实例或重复移动。
- 后续 Reset / Remake 可调用最小接口让 Shaker回 `Rest`，但本 Ticket 不实现完整 Remake 流程。
- 若移动带动画/过渡，用完成 Event / Callback 通知 Coordinator，最终回 `Stable`。

## Do Not
- 不做 Acquire Flair。
- 不做 Jigger → Shaker Transfer、Ice、Close、Shake、Taste、Pour。
- 不改 DrinkSystem、Gesture、P5-3 Outline 视觉。

## Acceptance
**Auto:** 编译通过；Rest 点击后只触发一次移动；完成后 Shaker=`Preparing`、Coordinator=`Stable`、PrimaryHeld 不被 Shaker占用；重复点击无异常；可正确重置到 Rest。  
**Manual:** 点击 Shaker 后能稳定移动到合理 Prep 位置，不挡主要操作区；再次点击不乱跳；原有 Bottle/Jigger/Measurement 无新增退化。

## Implementation Check — 2026-09-10

- 新增 `ShakerPreparation`，由 Shaker 自身保存 `Rest / Preparing / ReadyToShake / ShakeComplete`；本票仅进入 Rest / Preparing。
- Coordinator 在空手、Stable 时响应配置 Shaker 的普通点击，直接移动到 Prep Position 并进入 Preparing，不创建实例、不占用 Held。按 SPEC 2.2，已有 Bottle / Jigger Held 时先 RMB 释放占用。
- 使用即时移动，无过渡动画或定时等待。Preparing 重复点击仍由 Coordinator 消费，不进入旧桌面拖拽 / Transfer。
- `ShakerPrepPosition` 位于 Scene 根层级，World Position `(0.16, 1.61, 0.55)`，Rotation `(0, 0, 0)`；保留原桌面高度。运行起点 Rest Position 为 `(0, 1.61, 0.60)`。
- `ResetToRest()` 仅恢复本次运行初始位置 / 旋转及 Rest 状态，不清空液体、不执行完整 Remake，也未接入旧 R 键流程。
- Scene 停用该 Shaker 的旧 `ShakerController`，避免 Acquire 位移累计旧 ShakeLevel；DrinkSystem 脚本保持不变。
- Automated Check：PASS。编译通过；三轮 Acquire / 重复拒绝 / 精确 Reset、单实例、Stable、Held 空、忙碌 / 占用拒绝、液体数据不变、Prep 可见 Bounds 和摄像机射线可达性均通过。跨帧保持 Preparing。
- T03 Jigger 回归及 T04 六瓶 Measurement 回归 PASS；运行 Console 无 Error / Exception，原有场景配置警告保留。
- 检查入口：初始 Rest、空手的 Play Mode，执行 `Tools/CupPrototype/Validate T05 Shaker (Play Mode)`；输出 `T05_AUTOMATED_PASS`。
- 未修改 Gesture、P5-3 Renderer / Shader、Bottle / Jigger Hold Anchor 或 UI；未实现开合盖表现、Transfer、Ice、Shake、Taste、Pour。
- Manual Check 待用户验证实际点击、Prep 位置和既有交互手感；尚不关闭 Ticket，不开始下一 Ticket。
