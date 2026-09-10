# T02 — Bottle Normal Acquire + Return

> Module: Interaction Core  
> Phase: TICKETS  
> Status: PASS / Closed  
> Depends on: `T01_InteractionCoordinator_Foundation.md`, `02_ARCHITECTURE.md`

## Goal

让 Bottle 首次接入新的 `InteractionCoordinator` 正式交互流程：

```text
Click Bottle
→ Normal Acquire
→ Bottle 到固定 Hold Anchor
→ PrimaryHeld = Bottle
→ RMB
→ Bottle 返回固定原 Slot
→ PrimaryHeld 清空
```

本 Ticket 只完成 Bottle 的 Normal Acquire / Held / Return 闭环。

## Scope

优先控制在 1～3 个主要文件：

- `InteractionCoordinator.cs`
- 必要的 Bottle / Hold 定位脚本（如确有需要，仅新增或修改 1 个）
- `SampleScene.unity`

Unity 自动生成 `.meta` 不计入主要文件数。

## Do

- 在 `SampleScene` 增加或复用一个明确的 Bottle Hold Anchor。
- Bottle 被正常点击时，由 Coordinator 执行 Acquire。
- Acquire 成功后：
  - Bottle 位于固定 Hold Anchor / Hold Pose。
  - `PrimaryHeld` 指向该 Bottle。
  - 不再以桌面 Mouse Drag 作为该 Bottle 的正式 Held 状态。
- Stable Held 状态下按 RMB：
  - Bottle 返回自己的固定原 Slot / Rest Position。
  - `PrimaryHeld` 清空。
  - `ActionState` 最终回到 `Stable`。
- Bottle Return 后再次点击，必须可以重复完成 Acquire → Return。
- 保留现有 Bottle 数据、Liquid、Tilt、Renderer / Shader 等能力。
- 如果移动过程需要等待现有动画/过渡完成，应使用明确完成 Event / Callback 收口；不要用硬编码等待时间。

## Slot / Position Rule

Bottle 必须回到固定站位。

优先复用场景中已经存在的 Bottle 固定位置 / Slot 结构。  
若当前项目没有独立 Slot 组件，本 Ticket 可采用最小实现保存 Bottle 的初始 Rest Transform，作为其固定返回位置。

不要建立通用 Inventory / Socket / Placement Framework。

## Input Rule

本轮仍使用现有 Input API。

- Coordinator 只接管本 Ticket 所需的 Bottle Click 与 Bottle Held 时 RMB Return。
- 不迁移 `InputSystem_Actions`。
- 旧 Debug 快捷键继续保留。
- 同一次输入不得同时触发旧 Bottle Drag 与新的 Bottle Acquire。

## Do Not

- 不实现 Jigger Acquire。
- 不实现 Secondary Held。
- 不实现 Bottle → Jigger Measurement。
- 不实现 Bottle Pour。
- 不实现 Acquire Flair。
- 不修改 Gesture Recognizer。
- 不接 Selection Outline 新状态源。
- 不实现 Shaker / Cup 流程。
- 不修改 DrinkSystem 的液体、Flavor、Transfer 规则。
- 不修改 P5-3 Renderer Feature / Shader / Outline 视觉。
- 不建立 Tool Handler、Dual Hand Framework、Inventory 或通用 Slot Framework。
- 不处理与本 Ticket 无关的旧 Drag 问题。

## Automated Check

必须满足：

1. Unity 编译 PASS，无新增编译错误。
2. `SampleScene` 中 Coordinator 引用有效。
3. Bottle Acquire 后 `PrimaryHeld == Bottle`。
4. Bottle Acquire 后位置 / 姿态到达配置的 Hold Anchor / Hold Pose。
5. RMB Return 后 `PrimaryHeld == null`。
6. RMB Return 后 Bottle 回到固定 Rest Slot / Rest Transform。
7. Acquire → Return 至少连续执行两轮，状态不残留。
8. 不产生 NullReference / Exception。
9. Jigger / Shaker / Cup 未接入新的 Held 流程。
10. Renderer / Shader 未修改。

### Automated PASS

以上全部满足才记为 Automated Check PASS。

## Manual Check

Play Mode 中验证：

1. 点击一瓶 Bottle，Bottle 应进入固定手持位置。
2. Bottle Held 后不应继续表现为桌面自由 Drag。
3. 按 RMB，Bottle 应稳定回到原固定位置。
4. 再次点击同一 Bottle，可重新拿起并再次归位。
5. 连续测试至少两瓶 Bottle，不能出现上一瓶仍残留为 `PrimaryHeld`。
6. 原有场景、UI、Flair 等未涉及功能无明显新增退化。
7. Console 无持续新增 Error / Exception。

### Manual PASS

以上全部满足才记为 Manual Check PASS。

## Completion Rule

### Implementation Check — 2026-09-09

- Automated Check PASS：Unity 6000.4.11f1 编译及 SampleScene Play Mode 检查通过。
- 两瓶各连续执行两轮 Acquire / Return，验证 PrimaryHeld、Hold / Rest 姿态、Stable、清空引用，并检查占用与忙碌状态拒绝。
- 自动检查调用与输入共用的 Acquire / Return 入口；实际鼠标 Click / RMB 与视觉体验的 Manual Check 已由用户确认 PASS，见最终关闭记录。
- 新 Held 入口仅接受 Bottle；无 Secondary Held、Pour、Measurement 或其他工具流程接入。Renderer / Shader 未修改。
- 可重复执行：Unity batchmode `-executeMethod InteractionCoordinatorBottleCheck.Run`；本次日志位于 `%TEMP%/CupPrototype-T02-check.log`。
- 主要实现涉及 Coordinator、SampleScene，以及旧 Manager / Drag 的最小输入让位检查；另保留 Editor 自动检查脚本。不进入 T03。

- Automated Check PASS + Manual Check PASS → Ticket PASS / Closed
- Automated PASS、Manual 尚未完成 → `Functional Closeout Pending`
- 任一核心流程失败 → Ticket FAIL，不进入 T03

### Final Closeout — 2026-09-09

- Automated Check：PASS。
- Manual Check：PASS，依据用户本次确认记录。
- Bottle Hold Anchor：`Main Camera/BottleHoldAnchor`，最终 Local Position 为 `(0.42, 0.36, 0.38)`；Local Rotation 保持 `(-37°, 0°, 0°)`。
- Vodka、Gin、Rum、Tequila、Whiskey、Brandy 六瓶共用同一 Anchor 验证通过，无每瓶独立 Offset。
- 六瓶跨帧 Acquire 后可见 Bounds 完整位于屏幕右下偏中区域；瓶盖未越顶、瓶底未出屏，Return 后均精确回原 Slot。
- Ticket：PASS / Closed。本次仅更新验收与关闭记录，不修改代码或 Scene，不开始下一 Ticket。

## Codex Implementation Boundary

本 Ticket 只让 Bottle 成为第一个进入新 Held 架构的对象。

不要顺手接入 Pour、Jigger、Measurement、Flair、Outline 或其他工具，也不要借此重构整个旧 Interaction 系统。
