# T15 — Shaker → Cup Auto Pour

> Status: Implemented / Automated PASS / Manual Pending

## Goal
实现最终出杯主链：`ShakeComplete Shaker → 当前 Serve Cup` 自动过滤倒酒，并在完成后让 Shaker 自动回 Rest。

## Scope
`InteractionCoordinator.cs`、`ShakerPreparation.cs`、必要的 Cup/Pour 最小脚本、`CurrentDrinkRecord.cs`、`SampleScene.unity`。

## Do
- **触发条件**
  - Shaker 必须为 `ShakeComplete`。
  - 当前存在一个 Serve Cup，杯子可有或没有 Serve Ice。
  - `ActionState = Stable`。
  - 玩家点击 ShakeComplete 的 Shaker时，将其进入短暂 Serve Held / Pour 准备状态；再点击当前 Serve Cup，开始自动 Pour。
  - 若 Shaker 已被主动 `OPEN` 回 `Preparing`、或内容修改后尚未重新 Shake，则不得 Pour。

- **Pour 动作**
  - `ActionState → AutoPour`。
  - Shaker 自动移动到杯子上方并执行一次可理解的“开口 / 过滤 / 倾倒”表现。
  - 这里的“开口”仅属于 Pour 动画表现，**不得把 Shaker 状态切回 `Preparing`**，也不得重新开放加料 / Taste / Process Ice。
  - 不要求玩家控制角度、流速或倒入量；一次点击后自动完成。
  - 动作完成通过 Event / Callback 收口。

- **液体规则**
  - 继续复用现有 DrinkContainer / Transfer 数据。
  - Pour 持续到：
    1. Shaker 为空；或
    2. 当前 Cup 达到自身容量上限。
  - Cup 实际接收多少，就只增加多少；不得溢出 Cup。
  - 如果 Cup 先满，Shaker 中剩余液体视为本轮 Waste：
    - 在 Shaker Auto Return / Reset 阶段清除；
    - 不偷偷塞进 Cup；
    - 本 Ticket 不做 Waste UI / 评分。
  - Serve Ice 不参与液体容量计算逻辑的改写；保持当前 Demo 规则。

- **完成后状态**
  - Pour 完成后：
    - Cup 保留实际收到的饮品，作为后续 Finished Drink / Submit 的数据来源；
    - 当前 Cup 锁定，不再允许更换；
    - Shaker 自动回 `Rest`；
    - Shaker 内剩余液体清空；
    - Shaker 的临时开口 / Pour 状态清理；
    - `ActionState → Stable`。
  - `CurrentDrinkRecord.SelectedGlass / ServeIce / ProcessIce / Tasted` 等制作事实继续保留，供后续评分与 Submit 使用。
  - R / 切单仍应能安全清理中途 AutoPour，不留下 Held / Busy / 液体残留。

- **交互边界**
  - 没有 Serve Cup：拒绝 Pour。
  - Shaker 非 `ShakeComplete`：拒绝 Pour。
  - 当前 Cup 已有液体时，不允许再次执行第二次完整 Shaker Pour。
  - Pour 过程中拒绝 Bottle/Jigger/Taste/Open/换杯等冲突输入。

## Do Not
- 不实现 Submit / Service Zone。
- 不实现最终评分或 Waste 扣分。
- 不把 Hawthorne / Fine Strainer 做成独立可交互工具；当前只作为自动过滤表现或场景道具。
- 不改 Gesture 阈值、Flavor 算法、P5-3 Outline / Shader。

## Acceptance
**Auto:** 编译通过；只有 `ShakeComplete + Serve Cup` 可 Pour；Cup 容量受限且不溢出；Shaker 空或 Cup 满时正确结束；Cup 满时剩余液体在 Shaker Reset 阶段清除；完成后 Shaker=Rest、Cup 保留实际饮品；冲突输入拒绝；T13/T14 直接依赖回归 PASS。

**Manual:** Shake 完后选择杯子（Serve Ice 可选）→ 点击 Shaker → 点击 Cup，可看懂自动过滤倒酒；Cup 不溢出；完成后 Shaker 自动归位，Cup 留在 Serve Position；原有 Taste / 选杯 / 双冰槽流程无新增退化。

## Implementation (2026-09-12)
> T15.1 supersedes the post-Shake entry below: at least two actual liquid Ingredients are required to Shake; successful Shake automatically enters Serve Held, including before Cup selection. Direct Cup click pours; the extra Shaker click is needed only after RMB return. OPEN requires returning from Serve Held first.
> T15.1 + updated T15 Automated Check: PASS (2026-09-12). Joint Manual Check pending; T15 remains open. Current scene emits a missing LiquidVisualController warning for CoupeGlass_01_Prop; not changed in T15.1.
- Coordinator routes ShakeComplete Shaker click to Primary Serve Held, reusing the existing Shake Position. Requires free hands and the current empty Serve Cup; RMB returns it to Prep while preserving ShakeComplete.
- Clicking the current Cup starts AutoPour. ShakerPreparation owns lid-gap/tilt, capacity-limited TransferTo updates, return and completion callback. Visual lid opening never enters Preparing.
- Shaker return clears remaining liquid without resetting CurrentDrinkRecord. PourCompleted locks the current Cup even if no liquid was available; ResetAttempt clears the completion fact.
- R / order switch cancel the coroutine before returning the Cup; partial Cup and Shaker contents are cleared, Held/Busy released and no delayed callback writes remain.
- Uses existing scene references; no SampleScene change required. No independent strainer tool or additional rendering system.

## Modified Files
- Assets/Scripts/Interaction/InteractionCoordinator.cs
- Assets/Scripts/Interaction/ShakerPreparation.cs
- Assets/Scripts/Interaction/CurrentDrinkRecord.cs
- Assets/Editor/InteractionAutoPourCheck.cs + .meta
- This ticket.

## Manual Check — Pending
1. Finish Shake, select any Rack Cup (Serve Ice optional), return Bottle/Jigger if held, click Shaker then the served Cup.
2. Check the short Serve Held pose and lid-gap/tilting/return presentation; verify the Cup remains clickable while the Shaker is held.
3. Try contents below and above Cup capacity; Cup retains the actual pour, Shaker returns empty to Rest, Cup cannot be replaced.
4. During Pour try RMB/Open/Taste/other tools; then separately test R and switching order midway. No Busy, Held or partial liquid should remain after reset.

Manual approval is required before PASS / Closed. No Submit or Waste UI/scoring implemented.

## Automated Check — PASS (2026-09-12)
- Unity compilation passed. InteractionAutoPourCheck.RunBatch passed: Rest/Preparing/Ready/reopened/no-Cup rejection; Serve Held/RMB; current-Cup-only Pour; capacity-limited actual liquid and Flavor; remainder retained until return; exact Rest pose; preserved facts; completed Cup locking; R/order/disable cancellation.
- InteractionCoordinatorCupCheck.RunBatch (T13): PASS.
- InteractionDualIceCheck.RunBatch (T14): PASS.
- No Error/Exception observed by the Play Mode checks. Existing obsolete Unity API warnings remain; no full Interaction Core regression run.
