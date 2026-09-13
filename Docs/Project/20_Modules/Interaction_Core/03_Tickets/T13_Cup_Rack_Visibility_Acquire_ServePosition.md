# T13 — Cup Rack Visibility + Cup Acquire → Serve Position

> Status: Implemented / Automated Check PASS / Manual Check Pending

## Goal

让当前杯槽进入主视野并可稳定点击；玩家从 Cup Rack 选择任意杯子后，杯子移动到固定 Serve Position。同步微调现有水槽 / Wash 区的可视位置，但不做正式场景美术重排。

## Scope

`InteractionCoordinator.cs`、必要的 Cup/Serve Position 脚本、`SampleScene.unity`。  
允许对现有 Cup Rack 与 Sink/Wash 区做最小位置调整；优先不改 Camera。

## Do

- **场景可视性**
  - 从当前恢复后的 SampleScene 基线开始。
  - 调整现有 Cup Rack，使当前可选杯型在 Game View 中主体可见、鼠标可稳定命中。
  - 同时让现有 Sink / Wash 区进入合理可视范围，至少能辨认其位置，为后续功能预留。
  - 不追求成品布局；只解决“看得到、点得到、不会遮挡主操作区”。
  - 调整后必须复查 Bottle、Jigger、Shaker、Ice Well 等既有交互物体，不能因新布局导致射线遮挡或失去点击。

- **Cup Acquire**
  - 在 `ActionState = Stable` 时允许从 Cup Rack 选杯，不依赖 Shaker 状态，也不要求 Open / Shake / Taste。
  - 点击任意 Cup：
    - 记录该 Cup 为当前 `SelectedGlass`；
    - Cup 从 Rack 移到固定 `ServePosition`；
    - 错杯也允许，不做硬性阻止；
    - 完成后 `ActionState → Stable`。
  - Cup 离开 Rack 后不得再次作为“Rack Flair Acquire”目标；本 Ticket 只实现 Normal Acquire。
  - 同一时间只允许一个当前 Serve Cup。若当前 Serve Cup 仍为空，点击另一只 Rack Cup 时允许更换：当前杯子先返回原 Rack Slot，新杯子再移动到 Serve Position，并更新 SelectedGlass。若当前 Serve Cup 已接收任何饮料，则拒绝更换，直到本杯流程结束或 Reset。
    - 空杯判断：以该 Cup 的实际液体量 CurrentVolume <= 0 为准。
    - 换杯本身不算配方内容变化，不影响 Shaker / Taste / Shake 状态。

- **数据**
  - `CurrentDrinkRecord.SelectedGlass` 记录实际选择的杯型 / Cup 标识。
  - R Reset 或切换 ActiveOrder 后，未完成杯子回到原 Rack Slot，并清除 `SelectedGlass`。
  - 本 Ticket 不要求 Order 中已有“推荐杯型”；若字段为空也应正常选杯。

- **位置原则**
  - `ServePosition` 位于主操作区可见位置，不能遮挡 Shaker Prep、Jigger/Bottle Hold 或 Measurement 核心视野。
  - Cup Rack / Sink 的移动优先通过场景对象 Transform 完成，不修改现有模型结构。
  - 若发现一个布局无法同时保证可视与可点击，先停止并汇报遮挡对象 / Raycast 命中结果，不自行大改 Camera 或 Collider。

## Do Not

- 不做 Serve Ice。
- 不做 Shaker → Cup Pour。
- 不做 Submit / Service Zone。
- 不做 Cup Flair Acquire。
- 不做完整 Workstation Layout Polish。
- 不改 Camera、Bottle/Jigger Hold Anchor、Measurement UI、P5-3 Outline 视觉。

## Acceptance

**Auto:** 编译通过；Cup Rack 当前可选杯子均可被射线命中；Bottle/Jigger/Shaker 等既有点击不受影响；选杯后进入 ServePosition 且 `SelectedGlass` 正确；当前 Serve Cup 为空时，允许更换其他 Rack Cup，并正确回收旧杯、更新 `SelectedGlass`；当前 Serve Cup 已有液体时，拒绝更换且数据不变；R/切单可复位；T10–T12 直接依赖回归 PASS。

**Manual:** Game View 中杯槽与水槽位置可辨认；杯子容易点击；选杯后移动到合理 Serve Position；原有 Bottle/Jigger/Shaker 操作不被场景调整破坏；无新增 Error。

## Implementation Report — Cleanup 2026-09-12

### Modified Files
- `Assets/Scenes/SampleScene.unity`
- `Assets/Editor/InteractionCoordinatorCupCheck.cs`
- This ticket.

### Current Layout
- Disabled legacy standalone `Cup_Test` and duplicate `HighballGlass_02_Prop` (instances retained for reversibility).
- Three active Rack cups remain: Coupe, Highball 01, Rocks. Coordinator Rack references contain only these three.
- Cup visible mesh center world X/Z: Coupe `(0.47, 0.88)`, Highball `(0.59, 0.84)`, Rocks `(0.71, 0.87)`. Existing height, rotation, Scale and BoxCollider geometry are preserved.
- ServiceZone and ServePosition retain the accepted positions. No Camera, Hold Anchor, Measurement UI, DrinkSystem or Outline changes.
- T14 removes the Shaker state gate from TryAcquireCup. Stable plus normal object/empty-swap validity remains required; Open, Shake and Taste are not prerequisites.

### Automated Check
- T13 asserts exactly three active FinalGlass instances, direct selection after successful Shake without Open/Taste, center plus four nearby ray hits at Rack/Serve, original main-tool clicks, empty replacement, nonempty rejection, R/order return.
- Batch checks configure a 1920×1080 Game View through UnityEditor.PlayModeWindow, without changing Camera. Default batch 640×480 does not match the established 16:9 Game View.
- Requested regression scope: T13 + T10/T12 only; no T11 or full regression.

### Manual Check — Pending
1. Confirm the legacy standalone cup and duplicate Highball are no longer visible; three distinct cups remain grouped on the Rack.
2. Directly click a Rack cup before Shake, Open or Taste. Verify ServePosition is clear and empty-cup replacement works.
3. Confirm normal Bottle/Jigger/Shaker/Ice Well operations and Sink/Wash visibility.
4. R / order switch returns the current cup to its Slot.

No Serve Ice, Pour, Submit or Cup Flair implementation. Ticket remains open pending manual approval.
### Cleanup Results — Automated PASS / Manual Pending
- T13, T10 and T12 batch checks PASS; no additional regression suite run.
- All three cups pass center plus four neighboring ray checks at Rack and ServePosition. Direct ShakeComplete selection without Open/Taste, empty swaps, nonempty rejection, R and order return passed.
- Scene comparison confirms zero changed Camera / Collider serialized blocks. Only cup activation, Rack references and remaining cup Positions changed after serialization cleanup.
- Initial batch 640×480 viewport checks failed before the matching Game View test resolution was configured; final runs passed without runtime Error / Exception. Existing Unity startup/assembly-discovery warnings remain outside gameplay changes.

- T14 confirmed: replacing an empty iced Serve Cup clears its ServeIce before returning it. The new Cup starts without Serve Ice; ProcessIce and Shaker preparation are unchanged. Positive liquid volume still prohibits replacement.
