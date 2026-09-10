# T09 — Shake Gesture → ShakeComplete

> Status: Implemented / Automated Check PASS / Manual Check Pending

## Goal
实现 `ReadyToShake → Shake Gesture → ShakeComplete`，并记录本轮 `ShakePerformed`。

## Scope
`InteractionCoordinator.cs`、`ShakerPreparation.cs`、现有 Gesture/Flair 必要最小接线、`CurrentDrinkRecord.cs`、`SampleScene.unity`。

## Do
- 当 Shaker=`ReadyToShake`、`ActionState=Stable` 时，手势必须从 Shaker 上开始；Coordinator 将本次 Gesture 解释为 **Shake Gesture**。
- 继续复用现有 Gesture 采样、Template、Recognizer，不复制第二套识别器。
- Gesture 成功：
  - `ActionState → Shake`
  - 根据识别结果播放对应固定 Shake 动画；动画时长由动作配置决定，不跟玩家绘制时长绑定。
  - 动画完成必须通过 Event / Callback 通知 Coordinator。
  - `CurrentDrinkRecord.ShakePerformed = true`
  - Shaker 状态 → `ShakeComplete`
  - Shaker 保持 Closed，并回到 Prep Position
  - 液体与 ProcessIce 保持不变
  - 最终 `ActionState → Stable`
- Gesture 失败：
  - 不播放成功 Shake 流程
  - `ShakePerformed` 保持 false
  - Shaker 保持 `ReadyToShake`
  - 玩家可立即重试
  - 可显示简短失败反馈
- 不同 Shake Gesture 当前只允许影响动画表现/未来叙事记录，不修改 Flavor、稀释、温度或最终品质。
- R Reset 后必须清除 `ShakePerformed` 并让 Shaker 回 Rest。
- 旧“按移动距离累计 ShakeLevel”的逻辑不得重新成为正式入口。

## Do Not
- 不做 `[OPEN SHAKER]`、Taste、Remake、Pour to Cup。
- 不做 Acquire Flair。
- 不改评分、Flavor、P5-3 Outline 视觉。

## Acceptance
**Auto:** 编译通过；只有 ReadyToShake 且手势从 Shaker 开始才触发；成功后 `ShakePerformed=true`、Shaker=`ShakeComplete`、液体/ProcessIce 保持；失败后状态不前进且可重试；R 可清除；T03–T08 回归 PASS。

**Manual:** Close 后在 Shaker 上完成一次成功手势，能看懂 Shake 动画且最终回 Prep；故意失败一次后仍可重试；原有 Measurement/Transfer/Ice/Close 无新增退化。

## Implementation Check — 2026-09-10

- ReadyToShake 时从 Shaker 上按住 LMB 绘制，松开识别；不要求 Space。复用现有采样、ShakerRoll_01 模板、Recognizer 和动作簿，未实现 Acquire Flair。
- 按 SPEC 2.2，开始其他工具流程前先 RMB 释放 Bottle / Jigger；Shake 仅在空手、Stable、ReadyToShake 时开始。采样期间 ActionState=Flair，识别成功后变为 Shake，PrimaryHeld 暂时指向 Shaker。
- SampleScene 为 Shaker 绑定现有 Boston Shaker 可见根节点和 Main Camera/ShakerShakeAnchor；ShakerRoll_01 对应固定 Shake 动画，动作配置时长 0.9 秒，与画线时长无关。
- 动画真实完成后通过回调结束动作：记录 ShakePerformed，进入 ShakeComplete，Closed 回 Prep，释放 Held，恢复 Stable。液体、Ingredient、Flavor、ProcessIce 不变。
- 失败保持 ReadyToShake，不记完成，可立即重试。采样与动画期间阻止 RMB、重复 Gesture、冲突工具操作及 Tab；R 复用 ResetCurrentDrink，取消未完成动作并清除制作事实，回 Rest。
- Automated Check：PASS。Unity 编译通过；实际 Scene 的起笔对象 / 状态 / Held 门控、现有模板识别、失败重试、配置时长、可见动画与完成回调、Closed / Prep / Held、精确液体及 Ice 保留、完成后 Reset / 动画中 Reset / 绘制中 Reset 均通过。日志：`%TEMP%/CupPrototype-T09.log`，标记 `T09_AUTOMATED_PASS`。
- T03–T08 回归：PASS。日志：`%TEMP%/CupPrototype-T09-Regression.log`，标记 `T08_AUTOMATED_PASS`（包含 T03–T07）。Renderer / Shader / P5-3、DrinkSystem、Gesture Template / Recognizer 未修改；旧 ShakerController 继续禁用。
- 可重跑 `InteractionCoordinatorShakeCheck.RunBatch`；批处理完成后退出，普通编辑器完成后仅退出 Play Mode。`SetupAndRun` 用于 Scene 配置，不作为常规验证入口。
- 测试未发现新增 Gameplay Error / Exception；日志仍有既有 Unity 授权 / entitlement / 网络信息及旧 API 弃用警告。
- Manual Check 待用户确认：配料后 RMB 放下工具 → CLOSE SHAKER → 从 Shaker 起笔绘制已有 ShakerRoll_01 环形手势并松开，观察 Shake 与回 Prep；短点按制造失败后重试；按 R 确认能重新制作，并复查 Measurement / Transfer / Ice / Close。Ticket 尚未 Closed。
