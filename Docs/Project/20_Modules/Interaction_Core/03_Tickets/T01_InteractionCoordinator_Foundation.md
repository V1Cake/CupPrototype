# T01 — Interaction Coordinator Foundation

> Module: Interaction Core  
> Phase: TICKETS  
> Status: PASS / Closed  
> Depends on: `02_ARCHITECTURE.md`

## Goal

建立最小版 `InteractionCoordinator`，作为后续 Interaction Core 的统一状态入口。

本 Ticket 只搭建基础骨架，不迁移现有正式交互流程，不改变当前 Demo 的玩家体验。

## Scope

建议仅涉及：

- 新建 `InteractionCoordinator.cs`
- `SampleScene` 中挂载并完成最小必要引用

尽量控制在 1～2 个文件；如 Unity Scene 序列化产生必要修改，可计入本 Ticket。

## Do

- 新建一个薄 `InteractionCoordinator`
- Coordinator 保存以下运行时引用：
  - `Selected`
  - `PrimaryHeld`
  - `SecondaryHeld`
- 上述对象状态应提供只读访问
- 增加最小 `ActionState`：
  - `Stable`
  - `Measurement`
  - `AutoTransfer`
  - `Flair`
  - `Shake`
  - `AutoPour`
- 初始状态必须为 `Stable`
- 提供后续 Ticket 可使用的最小状态设置 / 清理入口
- `SampleScene` 中只允许存在一个有效 Coordinator
- Coordinator 当前不得主动读取玩家输入
- 保证现有 `DrinkTestManager / DragController / Flair` 等流程继续正常运行

## Do Not

- 不迁移 Input System
- 不接管现有鼠标 / 键盘输入
- 不修改 Bottle、Jigger、Shaker、Cup 玩法
- 不实现 Hold Anchor / Hold Pose
- 不连接 Selection Outline
- 不实现 Measurement Overlay
- 不修改 `DrinkContainer` 液体 / Flavor / Transfer 逻辑
- 不修改 P5-3 Renderer Feature / Shader / Outline 视觉
- 不删除或重构旧 Debug 快捷键
- 不提前实现 Tool Handler、Dual Hand Framework 或大型状态机

## Automated Check

必须满足：

1. Unity 编译 PASS，无新增编译错误。
2. `SampleScene` 中存在且仅存在一个有效 `InteractionCoordinator`。
3. Play Mode 启动后 Coordinator 初始化正常，无 NullReference / Exception。
4. 初始 `ActionState == Stable`。
5. `Selected / PrimaryHeld / SecondaryHeld` 初始允许为空。
6. 本 Ticket 不产生 P5-3 Renderer / Shader 修改。

### Automated PASS

以上全部满足才记为 Automated Check PASS。

## Manual Check

进入当前 Demo Play Mode，快速验证：

1. 原有对象点击 / Drag / Flair 等已有能力没有因 Coordinator 的加入而失效。
2. Coordinator 不抢占现有输入。
3. 玩家体验与本 Ticket 前基本一致。
4. Console 无持续新增异常。

### Manual PASS

以上全部满足才记为 Manual Check PASS。

## Completion Rule

- Automated Check PASS + Manual Check PASS → Ticket PASS / Closeout
- Automated PASS 但尚未完成人工验证 → `Functional Closeout Pending`
- 任一核心检查失败 → Ticket FAIL，不进入下一 Ticket

## Codex Implementation Boundary

### Automated Check Closeout — 2026-09-09

- Unity 6000.4.11f1 编译及 SampleScene Play Mode 自动检查 PASS，检查进程结果为 0。
- 场景中仅一个有效 InteractionCoordinator；启动及持续 8 秒观察后均为 Stable，Selected / PrimaryHeld / SecondaryHeld 均为空。
- 无 C# 编译错误，Play Mode 检查期间无 Error / Exception；启动日志包含许可证连接重试错误，随后许可证更新成功并完成检查。Renderer / Shader 与 T01 前基线一致。本次复核未修改功能脚本或 Scene（Scene 时间戳早于批处理启动，为用户关闭编辑器前保存）。
- 按本次用户授权，Automated Check 全部通过后标记 PASS / Closed；本记录不将自动检查冒充人工交互验证。
- 验证日志：`%TEMP%/CupPrototype-T01-rerun.log`。临时检查脚本已移除。

本 Ticket 的目标只是把 Coordinator 的“空骨架”安全放进项目。

不要借此机会迁移旧交互、重构 `DrinkTestManager`、修改 Gesture、修改 DrinkSystem 或处理后续玩法。
