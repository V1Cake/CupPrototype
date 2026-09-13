# T16 — Finished Drink → Service Zone Submit

> Status: Implemented / Automated PASS / Manual Pending

## Goal
把已完成的 Serve Cup 提交到 Service Zone，形成 Finished Drink，并结束当前制作尝试。

## Scope
`InteractionCoordinator.cs`、`CurrentDrinkRecord.cs`、必要的 Service Zone / Cup 提交脚本、`OrderContext` 最小接线、`SampleScene.unity`。

## Do
- 只有当前 Serve Cup **已经实际收到液体**、`ActionState=Stable` 时，才允许进入最终提交。
- 玩家点击成品 Cup → Cup 进入短暂 Final Held；再点击现有/新增 `ServiceZone`：
  - Cup 自动移动到 Service Zone 固定位置；
  - 以该 Cup 的 `DrinkContainer` 作为 Finished Drink 实际液体来源；
  - `CurrentDrinkRecord.SelectedGlass / ProcessIce / ServeIce / Tasted` 等制作事实继续保留；
  - `CurrentDrinkRecord.Submitted = true`；
  - 当前制作尝试进入已提交状态，不允许继续加冰、换杯、再次 Pour 或继续修改该杯。
- 提交不需要第二次确认；点击 Service Zone 即完成。
- 提交时把 `ActiveOrder + CurrentDrinkRecord + Finished Cup DrinkContainer` 提供给现有 Round/Scoring 入口；本 Ticket 只完成正确数据交接，**不修改评分公式**。
- 空杯、未完成 Pour 的杯、Busy 状态、已 Submitted 状态点击 Service Zone均拒绝且不改数据。
- R / 切单仍能安全清理未提交流程；已提交后的下一订单生命周期继续由现有 DemoRoundManager / OrderContext 处理。

## Do Not
- 不重写评分系统。
- 不实现 Customer、端酒动画或正式服务演出。
- 不修改 Flavor、Gesture、Outline / Shader。
- 不做 Workstation Layout Polish。

## Acceptance
**Auto:** 编译通过；只有非空 Finished Cup 可 Submit；提交后 Cup 到 Service Zone、`Submitted=true`，Finished Drink 数据来自实际 Cup；重复提交/继续修改被拒绝；现有 Order/Scoring 入口收到正确对象；T15 直接依赖检查 PASS。

**Manual:** 完成一杯 → 点击成品杯 → 点击 Service Zone，一次完成提交；杯子位置和状态清楚；提交后不能继续操作该杯；现有订单/评分流程能继续，无新增 Error。

## Implementation (2026-09-13)
- Current nonempty Cup with PourCompleted enters Primary Final Held only in Stable with free hands. RMB returns it to Serve Position. Clicking the dedicated Drink Service Zone submits immediately without a second confirmation.
- Drink Service Zone is separate from the old ServiceZone object used by Cup Rack. New pad center is (0.20, 0.916, 0.65); DrinkServicePosition aligns the Cup bottom at (0.20, 0.920, 0.65). FinalCupHoldAnchor is under Main Camera at local (0.24, -0.19, 0.45); Camera settings and existing tool/Cup positions are unchanged.
- DemoRoundManager.TrySubmitFinishedDrink receives the exact ActiveOrder, CurrentDrinkRecord and Cup reference. FinishedDrink and SubmittedRecord expose this handoff. CurrentDrinkRecord.Submitted is set without clearing preparation facts or liquid.
- DrinkTestManager's existing score calculation / UI event body is reused through EvaluateFinishedDrink(cup, recipe). No scoring formula change or fallback container selection in the formal Submit path.
- Submitted blocks formal Interaction input and legacy mouse ownership. Existing R/order lifecycle resets the record and returns Cup from Final Held / submitted position. No transport animation or Customer system added.

## Modified Files
- Assets/Scripts/Interaction/InteractionCoordinator.cs
- Assets/Scripts/Interaction/CurrentDrinkRecord.cs
- Assets/Scripts/Game/DemoRoundManager.cs
- Assets/Scripts/DrinkSystem/DrinkTestManager.cs (explicit scoring data entry only)
- Assets/Scenes/SampleScene.unity
- Assets/Editor/FinishedDrinkSubmitCheck.cs + .meta
- This ticket.

## Manual Check — Pending
1. Finish AutoPour into any Cup, with no other object held; click the Cup, then the pad labelled SERVE.
2. Confirm the Cup moves to the pad, score feedback updates and further edits/submission are rejected.
3. Before submitting, test RMB returning to Serve Position and R/order switch clearing Final Held.
4. After submission, switch to the next order and verify preparation can start normally.

Not Closed until manual approval. No full Interaction Core regression run.

## Automated Check — PASS (2026-09-13)
- FinishedDrinkSubmitCheck.SetupAndRun: compilation and all three Cup submissions passed; empty/unfinished/Busy/wrong-zone/wrong-order/repeat rejection; Final Held RMB return; service-position alignment; actual Cup/order/record handoff; original scoring result; facts retained; Submitted edits blocked; R/order cleanup before Submit and next order after Submit.
- InteractionAutoPourCheck.RunBatch: T15.1 + T15 direct regression PASS, including Cup liquid visual checks.
- Main Camera captures inspected for Final Held and submitted Cup placement; Service Zone remains raycast-accessible while holding each Cup.
- No runtime Error / Exception observed by the checks. Existing Unity obsolete API warnings remain.
- Only necessary T16 Scene additions/references retained; unrelated serialization changes restored from the pre-T16 snapshot.
