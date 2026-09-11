# T11 — Minimal Order Context

> Status: Implemented / Automated Check PASS / Manual Check Pending  
> Note: 原 `T11_Taste_After_ShakeComplete.md` 暂停，后续顺延为 Taste Ticket。

## Goal
建立最小订单上下文，让 Interaction / Taste / Scoring 读取同一个 Active Order，并在新订单开始时创建新的 CurrentDrinkRecord。

## Scope
现有 `DemoRoundManager`、新增最小 `OrderContext`（或等价数据类）、`CurrentDrinkRecord.cs`，必要的场景接线。

## Do
- 用现有 DemoRoundManager 作为当前订单来源，建立唯一 `ActiveOrder`。
- ActiveOrder 至少暴露：Recipe / 目标配方、Expected Flavor（复用现有配方/评分依据计算）、推荐杯型、Process Ice / Serve Ice 要求（已有则接入，缺失可为空）、AttemptCount / RemakeOccurred。
- 新订单激活时创建/重置新的 `CurrentDrinkRecord`，旧制作事实不得串到下一单。
- Remake 暂只预留 `AttemptCount +1 / RemakeOccurred=true` 的最小接口，不实现完整 Remake UI。
- Interaction 只读取 ActiveOrder，不接管订单生命周期。
- 不开发 Customer System；未来 Customer 只负责把自己的 Order 交给 OrderContext 激活。

## Do Not
- 不实现 Taste、Remake UI、Submit、Scoring 新公式。
- 不开发 Customer / Queue / Economy。
- 不重构现有 DrinkSystem。

## Acceptance
**Auto:** 编译通过；始终只有一个 ActiveOrder；切换订单后 CurrentDrinkRecord 正确重建；旧制作事实不串单；现有 DemoRoundManager 流程保持可用。  
**Manual:** 切换两次订单，确认当前订单内容正确、制作记录会清空重建；无新增 Error。

## Implementation Check — 2026-09-11

- DemoRoundManager.ActiveOrder 为当前唯一激活订单上下文；启动选择和现有 SetTarget / StartNextTarget 路径激活新 OrderContext，继续同步原有订单 UI、评分目标和 Mixing 状态。
- OrderContext.Recipe 复用 TargetDrinkData，ExpectedFlavor 直接读取现有评分所用 targetFlavor，不增加风味计算或评分公式。
- 暴露 RecommendedGlass、ProcessIceRequired、ServeIceRequired；现有目标资产未提供这些字段，因此当前为 null（未知，而非不需要）。不使用 RecipeTerminal 的独立示例文案推断 Gameplay 要求。
- 新单调用 CurrentDrinkRecord.BeginOrder：绑定同一个 ActiveOrder 并清空 ProcessIce / ShakePerformed / Tasted。保留现有 MonoBehaviour 实例和引用，按 Ticket 允许的重置方式开启新制作记录；无需修改 SampleScene。
- 新单 AttemptCount=1、RemakeOccurred=false；RecordRemake 仅累加次数，RemakeOccurred 随次数反映是否重做，不实现制作清理、Remake UI 或订单推进。R 保留当前订单和计数，仍走已有制作 Reset。
- 即使关闭现有 clearContainersOnTargetSwitch 选项，切单仍取消旧制作动作并重置事实，避免旧回调污染新单；该选项仍控制是否清空液体。
- Automated Check：PASS。编译通过；唯一订单来源 / 制作记录、启动激活、两次切单、配方 / ExpectedFlavor / UI / 评分目标一致、制作事实隔离、空值要求、次数接口、R / Submitted / Mixing 原流程，以及关闭清液选项时取消旧动作均通过。日志 `%TEMP%/CupPrototype-T11.log`，标记 `T11_AUTOMATED_PASS`。
- 直接依赖 T10：PASS，涵盖最新的内容变化才使 Shake 失效规则，以及 Reopen / Ice / Reset。日志 `%TEMP%/CupPrototype-T11-T10Dependency.log`，标记 `T10_AUTOMATED_PASS`。未运行 Interaction Core Full Regression。
- 重跑入口：`MinimalOrderContextCheck.RunBatch`；普通编辑器需先退出 Play 并保存 Scene，检查完成后仅退出 Play；批处理完成后退出进程。
- 未修改 DrinkSystem、评分公式、Gesture、Outline 或 Scene。首轮检查脚本的属性名编译错误已修正，最终编译及检查通过；既有 Unity 服务 / 授权和旧 API 警告未处理。
- Manual Check 待确认：Play 后用 N 连续切换两次订单（或 1/2/3 选择），检查现有订单显示同步变化；切换前加冰 / Shake，切换后确认旧制作事实清空、Shaker 回 Rest 并可重新开始。Ticket 尚未 Closed，不开始下一 Ticket。
