# T04 — Bottle → Jigger Measurement

> Status: PASS / Closed

## Goal
实现 Bottle → Jigger 的正式测量倒酒流程，并补齐 `Jigger 单持 → 点击 Bottle` 的反向双持入口。

## Scope
`InteractionCoordinator.cs`、必要的 Measurement/UI 脚本、`SampleScene.unity`。  
允许对现有输入让位逻辑做最小修改。

## Do

1. **补齐反向双持入口**
   - 当 `PrimaryHeld = Jigger` 且当前 `ActionState = Stable` 时，点击 Bottle：
     - Bottle 成为 `PrimaryHeld`
     - Jigger 转为 `SecondaryHeld`
     - Bottle / Jigger 分别进入现有右手 / 左手 Hold Anchor
   - 已经处于 `Primary Bottle + Secondary Jigger` 时，点击其他 Bottle 不自动换瓶；维持当前持有关系。

2. **Measurement 必须由“瞄准 Jigger + 按住 LMB”触发**
   - 前置条件：
     - `PrimaryHeld = Bottle`
     - `SecondaryHeld = Jigger`
     - `ActionState = Stable`
     - 鼠标射线当前命中这个 Secondary Jigger
   - 满足条件后按下 LMB：
     - `ActionState → Measurement`
     - Bottle 自动进入对准 Jigger 的 Pour Pose
     - 开始以当前固定流速向 Jigger 写入液体
   - 鼠标指向桌面、Bottle、Shaker、Cup 或其他对象时按 LMB，不进入 Measurement。
   - Measurement 已开始后，不再要求鼠标持续停留在 Jigger 上；只检测 LMB 是否持续按住，避免鼠标轻微移动导致意外中断。

3. **Measurement 结束规则**
   - 松开 LMB → 立即停止倒酒。
   - Jigger 达到自身最大容量 → 自动停止，即使 LMB 仍按住。
   - 停止后：
     - Bottle 返回原 Bottle Hold Pose
     - Jigger 保持 Secondary Held
     - `ActionState → Stable`
     - Bottle 与 Jigger 的 Held 引用保持正确，不自动归位
   - 不根据 Recipe 目标量自动停止。

4. **液体数据继续走现有 DrinkSystem**
   - 当前 `PourableIngredient` 继续作为无限材料源，Bottle 不扣量。
   - Measurement 通过现有 `AddIngredient` 向 Jigger 写入对应 Ingredient / Amount。
   - 不复制一套 Measurement 专用液体数据。
   - Jigger 仍遵守现有有限容量。
   - 本 Ticket 不改变 Flavor、材料、Transfer 基础规则。
   - 不新增 Bottle Remaining Volume、Empty State、Refill 或 Inventory，不修改 `DrinkContainer.TransferTo` 来适配 Bottle → Jigger。
   - 正式版如需瓶耗尽，另行设计 Bottle Capacity / Inventory。

5. **Measurement Overlay**
   - Measurement 开始时显示，结束时隐藏。
   - 世界画面使用暗色半透明遮罩弱化，但仍能辨认场景。
   - 放大的 Jigger 位于画面中央偏下，主体约占屏幕高度 45%～55%。
   - Overlay 中显示：
     - 放大的 Jigger
     - 实时变化的液位
     - 清晰、高对比度的测量刻度
     - 少量必要容量刻度标记
   - 不显示实时 ml 数字。
   - 不显示 Recipe Target Line。
   - 不提示“正确量”，玩家根据 Recipe 自行判断。
   - 视觉风格沿用当前 dark bar / pixel UI，不新建另一套视觉语言。
   - 现有 Recipe / Order 信息尽量保持可读。

6. **Camera Lock**
   - Measurement 期间禁止玩家移动 Camera。
   - Recipe Terminal / Display Focus 等已有相机入口也不能绕过该锁定。
   - Measurement 结束后恢复原 Camera 控制。
   - Camera 只执行 Lock / Unlock，Measurement 是否开始和结束由 Coordinator 决定。

## Do Not
- 不做目标量自动停止、Target Line、实时 ml 数字。
- 不做 Bottle→Cup/Shaker、Jigger→Shaker。
- 不改 Gesture、评分、Flavor、P5-3 Outline 视觉。

## Acceptance
**Auto:** 编译通过；反向双持正确；只有瞄准 Secondary Jigger 才能开始 Measurement；LMB 松开/满容量正确停止；Jigger 的 Ingredient / Amount 写入及容量上限、ActionState、Overlay、Camera Lock/Unlock 正确；Bottle 保持无限材料源；无异常。  

**Manual:** 倒酒触发不误操作；Overlay 清晰且符合上述布局；液位实时变化；Camera 确实锁定；松开后能立刻恢复正常操作。

## Implementation Check — 2026-09-10

- 已按产品修正采用无限 Bottle 材料源；只调用现有 `DrinkContainer.AddIngredient` 写入 Jigger，未新增瓶容量或库存，未修改 `TransferTo`。
- Automated Check：PASS。Unity 编译通过；六瓶分别通过反向双持、错误射线目标拒绝、固定流速 / Ingredient / Amount、容量截断、松手 / 满杯停止、Overlay 与 Camera Lock、Bottle 数据不变及精确 Return 检查。
- Play Mode 跨帧检查：释放 LMB 后由实际 Update 停止 Measurement、隐藏 Overlay、解锁 Camera；T03 原有单持和两轮双持 Return 回归 PASS。
- 检查入口：空手且 Jigger 为空的 Play Mode，执行 `Tools/CupPrototype/Validate T04 Measurement (Play Mode)`；结果为 `T04_AUTOMATED_PASS`。
- Overlay 采用量杯剖面显示，主体高度为屏幕约 50%，中央偏下；只显示实时液位与固定容量刻度，不显示实时 ml 或 Recipe Target Line。Game View 截图检查及用户 Manual Check 均通过。
- SampleScene 仅新增 Measurement Overlay / Camera Lock 组件及 Coordinator 引用；既有两个 Hold Anchor 未调整。
- 本轮对照检查：DrinkContainer、PourableIngredient、Gesture、Renderer / Shader 未修改。DrinkTestManager 仅在已有 Reset 入口结束 Measurement，防止清空后继续写入。
- 运行检查无新增 Error / Exception。现有 6 条场景配置警告（旧 Debug UI、评分 / 订单引用和测试 Flair 配置）保留，不在本 Ticket 修复。
- Manual Check：PASS（2026-09-10，用户确认）。实际双向 Acquire、瞄准按住 LMB、移开鼠标持续倒入、松手 / 满杯停止、两次 RMB Return 验证通过；Measurement Overlay / Camera Lock / Tab 互斥验证通过。
- Automated Check PASS + Manual Check PASS → T04 PASS / Closed。本次仅更新验收记录，不修改代码或 UI，不开始下一 Ticket。
