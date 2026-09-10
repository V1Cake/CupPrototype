# Interaction Core — 02_ARCHITECTURE

> Status: Architecture Approved / Ready for Tickets  
> Scope: Interaction Core only  
> Purpose: 固化 Interaction Core 的职责边界、状态归属与系统连接方式。本文不拆 Ticket，不包含实现代码。

## 1. 总体结构

Interaction Core 采用 **Thin Interaction Coordinator** 方案。

```text
Player Input
    ↓
Interaction Coordinator
    ├─ 判断当前对象 / 当前阶段 / 当前 Action
    ├─ 判断交互是否合法
    ├─ 发起具体动作
    └─ 接收动作完成 Event / Callback
            ↓
    ┌───────────────┬─────────────────┬─────────────────┐
    ↓               ↓                 ↓                 ↓
DrinkSystem      Shaker State      Gesture          UI / Camera
液体与容量        Shaker生命周期      识别手势          执行显示/锁定
    ↓
CurrentDrinkRecord
    ↓
Order / Scoring
```

Coordinator 只负责 **输入路由、合法性判断、动作切换与流程协调**。  
液体、动画、Gesture 识别、评分、订单生命周期等具体能力继续由对应模块负责。

如果未来某一工具的交互规则明显复杂，再从 Coordinator 中拆出独立 Tool Handler；当前 Demo 不提前拆 Bottle / Jigger / Shaker / Cup Handler。

---

## 2. Interaction Coordinator 职责

Coordinator 持有：

- 当前 `Selected`
- 当前 `Primary Held`
- 当前 `Secondary Held`，MVP 仅允许 Jigger
- 当前全局 `ActionState`
- 对 Shaker 状态、CurrentDrinkRecord 等系统的引用

Coordinator 负责：

1. 接收正式玩家交互输入。
2. 根据目标对象与当前状态决定动作。
3. 在动作开始前完成合法性检查。
4. 将具体执行交给 DrinkSystem、Shaker、Gesture、UI、Animation 等模块。
5. 在 Event / Callback 返回后结束动作并切换状态。
6. 为 Outline 等系统提供统一的 Selected / Held 状态来源。

Coordinator 不负责：

- 液体数据计算
- Flavor 计算
- 评分
- Gesture 模板识别算法
- 动画具体表现
- 顾客生命周期
- 完整 Order 生命周期

---

## 3. Held 模型

当前 MVP 不建立通用双手系统。

```text
Primary Held
Secondary Held
```

规则：

- 通常只有一个 Primary Held。
- 唯一持续双持例外：`Primary Bottle + Secondary Jigger`。
- Jigger 可作为 Secondary Held 参与 Measurement。
- 支持手可以参与动画表现，但不额外建立两套通用 Hand State Machine。
- RMB Return 遵循既定产品规则：双持时优先返回 Primary Bottle，再返回 Secondary Jigger。

Held 对象使用固定 Hold Anchor / Hold Pose，不继续沿用桌面 Mouse Drag 作为正式 Held 机制。

---

## 4. ActionState

Coordinator 使用一个小型全局动作枚举，避免多个流程同时抢输入。

建议状态：

```text
Stable
Measurement
AutoTransfer
Flair
Shake
AutoPour
```

它只表示“玩家当前正在执行什么动作”，不扩展为大型状态机。

动作执行期间，根据 SPEC 禁止不允许的 RMB、重复点击、其他 Gesture 或冲突输入。

---

## 5. Shaker 状态归属

Shaker 自己保存生命周期状态：

```text
Rest
Preparing
ReadyToShake
ShakeComplete
```

Coordinator 读取并驱动这些状态，但不把 Shaker 生命周期复制到自己内部。

核心关系：

```text
Rest
→ Acquire / Acquire Flair
→ Preparing
→ Close
→ ReadyToShake
→ Shake Gesture Success
→ ShakeComplete
```

Taste 前允许 Open：

```text
ReadyToShake → Preparing
ShakeComplete → Preparing
```

如果从 `ShakeComplete` 重新打开，需要清除 `ShakePerformed`，后续重新 Close + Shake。

Taste 后禁止重新打开或继续加料。

---

## 6. Gesture 架构

继续复用现有：

- Gesture 采样
- Gesture Template
- Recognizer
- Flair 动画能力

Recognizer 只回答“识别到了什么手势”。

**Gesture 的用途由 Interaction Coordinator 根据当前交互状态决定。**

示例：

```text
Shaker Rest + Gesture
→ Acquire Gesture

Shaker ReadyToShake + Gesture
→ Shake Gesture
```

本轮不为 Acquire / Shake 复制两套 Recognizer，也不要求修改为大型 Gesture Purpose 资源体系。

Acquire Gesture 失败：回退 Normal Acquire。  
Shake Gesture 失败：保持 ReadyToShake，允许重试。

---

## 7. 动作完成机制

所有持续动作统一通过 **Event / Callback** 通知 Coordinator 完成。

适用于：

- Acquire Flair
- Shake
- AutoTransfer
- AutoPour
- Shaker Auto Return
- 其他需要等待动画或流程完成的动作

原则：

```text
Coordinator 发起动作
→ 对应模块执行
→ 实际完成
→ Event / Callback
→ Coordinator 回到 Stable / 进入下一阶段
```

Coordinator 不依赖硬编码等待时间猜测动画结束。

---

## 8. Measurement Overlay

职责分离：

- Coordinator：决定进入 / 退出 Measurement。
- Measurement UI：只显示放大的 Jigger、实时液位与刻度。
- Camera Controller：执行 Camera Lock / Unlock。
- DrinkSystem：继续提供实际 Jigger 当前量 / 最大容量。

Measurement 开始后：

```text
ActionState = Measurement
Camera Locked
Overlay Active
```

结束后由 Coordinator 统一恢复。

Recipe Terminal / Display Focus 等已有相机入口必须考虑互斥，不能绕过 Measurement Camera Lock。

---

## 9. Shaker Capacity

Shaker 采用明确的 **Unlimited Receive** 配置。

- Bottle / Jigger / Cup 保留现有正常容量逻辑。
- Shaker 接收 Jigger 内容时不因普通 maxVolume 拒绝转移。
- 不使用极大伪容量值。
- 本轮不建立通用 Capacity Policy 框架。
- 将来正式玩法需要 Shaker 容量限制时，可关闭该配置并恢复有限容量。

---

## 10. CurrentDrinkRecord

建立独立的 `CurrentDrinkRecord / Preparation Record`，生命周期覆盖“一次制作尝试”。

记录至少包括：

```text
ProcessIce
ShakePerformed
SelectedGlass
ServeIce
Tasted
Submitted
Current Preparation Stage
```

原则：

- Shaker Auto Reset 不清除此 Record。
- Taste、Pour、Submit、Scoring 可继续读取。
- Remake 时清空并创建新的制作尝试。
- 具体液体组成仍以 DrinkContainer / Finished Drink 为事实来源，不重复复制整套液体系统。

---

## 11. Order 与未来 Customer 边界

目标关系：

```text
Customer
  ↓
Order
  ↓
CurrentDrinkRecord
  ↓
Finished Drink
  ↓
Scoring / Submit
```

Order 层负责：

- 当前订单
- 配方要求
- 订单状态
- `AttemptCount`
- `RemakeOccurred`

Remake：

```text
保留当前 Order
AttemptCount + 1
重置 CurrentDrinkRecord
重置当前未完成制作状态
```

当前 Demo 继续复用现有 `DemoRoundManager` 承担已有订单流程。  
本轮只固化边界，不开发完整 Customer System / 正式 OrderSystem。

---

## 12. Selection Outline

P5-3 视觉实现保持 Frozen。

仅调整状态来源：

```text
SelectionHighlight
← Interaction Coordinator
   ├─ Selected
   ├─ Primary Held
   └─ Secondary Held
```

不得修改：

- Renderer Feature
- Outline Shader
- Thickness
- 颜色与现有视觉规则
- 材质 / Liquid / Transparency 表现

不再依赖 DragController 来伪造新 Held 状态。

---

## 13. Input 策略

本轮继续使用现有 Input API。

- 新正式交互统一从 Interaction Coordinator 进入。
- 不迁移 `InputSystem_Actions`。
- 旧 Prototype / Debug 快捷键暂时保留。
- T / F / R、切订单等调试入口可以继续服务开发验证。
- 旧入口不得与 Coordinator 同时修改同一正式玩家交互状态。

正式版本再单独规划 Input System Migration 与 Debug Entry 清理。

---

## 14. 现有系统复用与退出边界

继续复用：

- `DrinkContainer` 的液体、材料、容量、Transfer 与 Source Deduction
- Flavor 数据与计算
- Cup / Jigger 有限容量
- Gesture Template / Recognizer
- FlairableTool 动画基础
- DemoRoundManager 当前订单与评分基础
- GameplayUIBridge
- P5-3 Outline 视觉实现

逐步退出正式玩家流程：

- Mouse Drag = Held
- Shift Transfer Source
- Remote Bottle Pour
- Bottle → Cup
- Jigger → Cup
- Movement-distance ShakeLevel
- T / F / R 等 Prototype 玩家捷径
- 使用“场景第一个 Container”作为 Taste / Score 目标的旧 fallback

这些旧能力本轮是否删除由后续 Ticket 决定；Architecture 只规定它们不能继续作为新正式流程的事实来源。

---

## 15. 架构约束

后续 Ticket 必须遵守：

1. 不把 Interaction Coordinator 做成新的大型 Manager。
2. 不重写 DrinkSystem 基础液体系统。
3. 不建立通用 Dual Hand Framework。
4. 不建立大型状态机框架。
5. 不建立通用 Capacity Policy。
6. 不修改 P5-3 Outline 视觉效果。
7. 不在 Interaction 内硬编码评分惩罚值。
8. 不提前实现 Top-up、Garnish、Stir、Wash、正式 Customer System。
9. 所有持续动作必须有明确完成 Event / Callback。
10. 每个实现 Ticket 必须分别定义 Automated Check 与 Manual Check。

---

## Architecture Result

**PASS — Ready for Ticket Breakdown**

当前 Interaction Core 的职责边界、状态归属、数据记录方式与现有系统复用策略已经明确，可进入下一阶段的 TICKETS 拆分。
