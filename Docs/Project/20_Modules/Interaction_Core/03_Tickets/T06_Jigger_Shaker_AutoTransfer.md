# T06 — Jigger → Shaker AutoTransfer

> Status: Functional Closeout Pending — Automated PASS / Manual Pending

## Goal
实现 `Held Jigger → Preparing Shaker` 的一键全量转移，并接入 Shaker 的 Unlimited Receive 规则。

## Scope
`InteractionCoordinator.cs`、`ShakerPreparation.cs` / 必要 DrinkContainer 最小修改、`SampleScene.unity`。

## Do
- 前置条件：`PrimaryHeld = Jigger`，或 `Primary Bottle + Secondary Jigger`；Shaker=`Preparing`、`ActionState=Stable`。点击 Shaker 后：
  - `ActionState → AutoTransfer`
  - Jigger 自动执行一次 Pour/Transfer 动作
  - 将 Jigger 当前全部内容转入该 Shaker
  - Jigger 清空，保持原 Primary / Secondary Held；双持时 Bottle 保持 Primary
  - 动作完成后 Jigger 回 Hold Pose，`ActionState → Stable`
- 只触发一次，不需要持续按住 LMB，也不做第二次精度操作；空 Jigger 点击 Shaker不得产生错误或重复数据。
- 液体数据继续复用现有 DrinkContainer / Ingredient 数据；转移前后总量必须守恒。
- Shaker 接收使用明确的 `Unlimited Receive` 配置：Jigger→Shaker 不受普通 maxVolume 拒绝；不要用“超大假容量”，也不要建立通用 Capacity Policy。
- Shaker 非 `Preparing`、Jigger 不处于上述合法 Held 组合、系统 Busy 时都应在数据变化前拒绝。
- 双持时只转移 Secondary Jigger 内容，不要求先放下 Bottle；回到 Stable 后可立即再次 Measurement。Bottle 内容不直接进入 Shaker。
- 若 AutoTransfer 有移动/动画，完成必须用 Event / Callback 收口。

## Do Not
- 不做 Jigger→Cup、Bottle→Shaker。
- 不做 Ice、Close、Shake、Taste、Pour to Cup。
- 不改 Gesture、评分、P5-3 Outline 视觉。

## Acceptance
**Auto:** 编译通过；Jigger 全量进入 Preparing Shaker、Jigger 清空、总量守恒、超普通 Shaker maxVolume 仍可接收；Primary Jigger 流程保持；双持连续执行 Measurement → Transfer → Measurement → Transfer，Bottle Primary / Jigger Secondary 保持正确；空 Jigger 不增加数据；非法状态无数据变化；T03/T04/T05 回归 PASS。  
**Manual:** 不放下 Bottle，Measurement 后点击 Preparing Shaker 全量倒入，连续加两份以上；Bottle / Jigger 保持双持，无状态残留或 Console 新错误。

## Implementation Check — 2026-09-10

- Coordinator 路由 Primary Jigger 或 Primary Bottle + Secondary Jigger 点击 Preparing Shaker，进入 AutoTransfer。ShakerPreparation 执行短暂倾倒 / 回手过渡，在倾倒位置调用一次现有 `TransferTo`，实际返回 Hold Pose 后回调 Coordinator 恢复 Stable。
- 全程保留原 Held 组合；不依赖持续按住 LMB。忙碌、错误目标、Rest、无 Held Jigger、空 Jigger 及重复调用在数据写入前拒绝，RMB 不打断动作。
- DrinkContainer 新增明确 `unlimitedReceive` 配置，只对 Shaker 接收 Jigger 生效；TransferTo 在此条件下绕过普通容量截断。其他来源限制、有限容器容量、材料比例和 Flavor 计算保持原逻辑。
- SampleScene 的 Shaker 启用 Unlimited Receive，原始 `maxVolume = 300` 保持不变；Cup / Jigger 配置为关闭。不使用超大容量或通用 Capacity Policy。
- Automated Check：PASS。Unity 编译通过；8 次真实跨帧全量转移累计 400 ml，超过原 300 ml 容量仍可接收；每次源量 / 材料清空、目标累计量 / 材料守恒、Held / Hold Pose / 回调状态正确，重复及非法输入无数据变化。关闭配置后的有限容量截断 / 满杯拒绝检查通过。
- T03、T04、T05 回归 PASS；运行 Console 无 Error / Exception。旧场景 UI / 订单 / 评分 / Flair 配置警告保留，不在本票修复。
- 可重复检查：初始 Rest、空手、空 Jigger / Shaker 的 Play Mode，执行 `Tools/CupPrototype/Validate T06 AutoTransfer (Play Mode)`；完成后输出 `T06_AUTOMATED_PASS`。
- 未修改 Gesture、评分、P5-3 Renderer / Shader、UI 或 Hold Anchor；未实现 Ice、Close、Shake、Taste 或 Pour to Cup。Shaker 开合盖视觉仍未接入。
- Manual Check 待用户确认：先取得 Shaker 到 Preparing，保持 Bottle + Jigger 双持，Measurement 后点击 Shaker 自动全量倒入并回手；不放下 Bottle，连续量取并转移两份以上。尚不标记 PASS / Closed，不开始下一 Ticket。

### T06 Correction — 2026-09-10

- 用户确认双持可直接转移 Secondary Jigger，取代原先必须先 RMB 归还 Bottle 的限制。
- 本次只调整 Coordinator 的 Shaker 点击路由 / Held 合法性、自动检查及本 Ticket；不修改 Measurement、容量、Gesture 或 Scene。
- Correction Automated Check：PASS。原 Primary Jigger 流程及连续 3 轮双持 Measurement → Transfer 均通过；每轮 Bottle 保持 Primary、Jigger 清空并保持 Secondary、完成后 Stable 且可再次 Measurement。累计量 / 材料守恒，Bottle 数据不变，空 Jigger 和仅持 Bottle 不增加数据。运行 Console 无 Error / Exception。
