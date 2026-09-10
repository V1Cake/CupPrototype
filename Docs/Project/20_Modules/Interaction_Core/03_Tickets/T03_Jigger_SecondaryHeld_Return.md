# T03 — Jigger Acquire + Secondary Held + Return

> Module: Interaction Core  
> Phase: TICKETS  
> Status: Functional Closeout Pending  
> Depends on: `T01_InteractionCoordinator_Foundation.md`, `T02_Bottle_NormalAcquire_Return.md`, `02_ARCHITECTURE.md`

## Goal

接入 Jigger 的正式 Held 流程，并实现当前 MVP 唯一允许的持续双持关系：

```text
Jigger Alone
→ Click Jigger
→ PrimaryHeld = Jigger

Bottle Held
→ Click Jigger
→ PrimaryHeld = Bottle
→ SecondaryHeld = Jigger
```

同时完成 RMB Return 的既定顺序。

## Scope

优先控制在 1～3 个主要文件：

- `InteractionCoordinator.cs`
- 必要的 Jigger / Hold 定位脚本（仅在确有需要时）
- `SampleScene.unity`

允许对已有旧输入脚本做最小“让位检查”，但不得扩展为整体重构。

## Do

- 为 Jigger 配置固定 Hold Anchor / Hold Pose。
- Jigger 单独点击时：
  - Jigger 进入固定 Held 位置。
  - `PrimaryHeld = Jigger`。
- Bottle 已为 PrimaryHeld 时点击 Jigger：
  - Bottle 保持 `PrimaryHeld`。
  - Jigger 进入 Secondary Hold 位置。
  - `SecondaryHeld = Jigger`。
- 禁止形成任意双持：
  - Jigger + Jigger 不允许。
  - Bottle 以外对象 + Jigger 不在本 Ticket 扩展。
  - 两个 PrimaryHeld 不允许。
- RMB Return：
  - 若 `PrimaryHeld = Bottle` 且 `SecondaryHeld = Jigger`，第一次 RMB 只返回 Bottle，Jigger 保持 Held，并转为当前主要持有对象。
  - 再次 RMB 返回 Jigger并清空 Held。
  - 若仅 Jigger Held，RMB 直接返回 Jigger。
- Return 后对象回固定原 Slot / Rest Transform。
- Acquire / Return 可重复执行，不残留状态。
- 保留现有液体、容量、Renderer / Shader、Gesture 等能力。

## Hold Rule

当前仍采用固定 Anchor / Pose。

建议：

- Bottle 继续使用 T02 已验证通过的 `BottleHoldAnchor`。
- Jigger 使用独立的 `JiggerHoldAnchor`。
- Secondary Jigger 应位于屏幕左侧或左下偏中区域，为后续 Bottle → Jigger Measurement 留出视觉空间。

本 Ticket 只要求 Held 位置清晰可见、无遮挡严重问题；不实现 Measurement Overlay。

## Input Rule

继续使用现有 Input API。

- Coordinator 接管 Jigger Acquire / Return。
- 旧 Drag 不得与新 Jigger Held 同时生效。
- 旧 Debug 快捷键保留。
- 不迁移 `InputSystem_Actions`。

## Do Not

- 不实现 Bottle → Jigger Pour。
- 不实现 Measurement Overlay。
- 不修改 Jigger 容量规则。
- 不实现 Jigger → Shaker AutoTransfer。
- 不实现 Acquire Flair。
- 不接 Selection Outline 新状态源。
- 不实现通用双手系统。
- 不建立 Hand State Machine。
- 不修改 P5-3 Renderer / Shader。
- 不开始 Shaker / Cup 流程。

## Automated Check

必须满足：

1. Unity 编译 PASS。
2. Jigger 单独 Acquire 后 `PrimaryHeld == Jigger`。
3. Jigger 单独 RMB Return 后 Held 清空并精确归位。
4. Bottle Held 时 Acquire Jigger：
   - `PrimaryHeld == Bottle`
   - `SecondaryHeld == Jigger`
5. 双持状态下第一次 RMB：
   - Bottle 归位。
   - Jigger 仍保持 Held。
   - Held 状态无重复 / 悬空引用。
6. 第二次 RMB 后 Jigger 归位，Held 全部清空。
7. 至少连续执行两轮完整双持 → Return 流程。
8. 不允许生成两个 PrimaryHeld。
9. 无新增 NullReference / Exception。
10. Renderer / Shader 未修改。

### Automated PASS

以上全部满足才记为 Automated Check PASS。

## Manual Check

Play Mode 中验证：

1. 单独点击 Jigger，确认进入固定 Hold 位置。
2. RMB 后 Jigger 正常回原位。
3. 先拿 Bottle，再点击 Jigger：
   - Bottle 仍在右侧 Held。
   - Jigger 出现在左侧 / Secondary Hold 位置。
   - 两者同时可见，无遮挡严重问题。
4. 双持时第一次 RMB：
   - Bottle 返回原 Slot。
   - Jigger 继续 Held。
5. 第二次 RMB：
   - Jigger 返回原 Slot。
6. 重复双持流程至少两轮，无状态残留。
7. 原有 UI / Flair 等未涉及功能无明显新增退化。
8. Console 无新增持续 Error / Exception。

### Manual PASS

以上全部满足才记为 Manual Check PASS。

## Completion Rule

### Implementation Check — 2026-09-09

- Automated Check：PASS。Unity 编译成功；Play Mode 单独 Jigger Acquire / Return 与两轮完整双持 / Return 检查通过，无新增运行异常。
- 按本 Ticket 的明确规则：单独 Jigger 为 Primary；双持第一次 RMB 后 Bottle 归位，Jigger 从 Secondary 提升为 Primary，Secondary 清空；第二次 RMB 后全部归位并清空 Held。
- 非配置对象、重复 Jigger、占用 Primary 和忙碌状态 Return 均受拒绝检查；Jigger 液体 / 容量数据保持不变。
- `Main Camera/JiggerHoldAnchor`：Local Position `(-0.28, 0.08, 0.42)`，Local Rotation `(-37°, 0°, 0°)`。已验证可见 Bounds 完整位于左侧视口；双持时 Jigger / Bottle 中心视线未命中其他 Collider。
- BottleHoldAnchor、Renderer / Shader 保持不变；未接入 Pour、Measurement、Transfer、Flair 或 Outline 新状态源。
- 可重复检查：进入未暂停的 Play Mode、双手为空时，执行 `Tools/CupPrototype/Validate T03 Jigger (Play Mode)`；结果输出 `T03_AUTOMATED_PASS`。
- Manual Check 尚待用户验证实际 Click / RMB、双持视觉与原有 UI / Flair。当前不关闭 Ticket，不进入下一 Ticket。

- Automated Check PASS + Manual Check PASS → Ticket PASS / Closed
- Automated PASS、Manual 尚未完成 → `Functional Closeout Pending`
- 任一核心流程失败 → Ticket FAIL，不进入下一 Ticket

## Codex Implementation Boundary

本 Ticket 只建立 Jigger Held 与 `Bottle + Secondary Jigger` 这一条明确双持特例。

不要提前实现 Measurement、Pour、AutoTransfer、Flair、Outline 或通用双手框架。
