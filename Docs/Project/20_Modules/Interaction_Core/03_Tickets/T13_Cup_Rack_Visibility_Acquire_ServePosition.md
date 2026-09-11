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
  - 仅在 `Shaker = ShakeComplete` 且 `ActionState = Stable` 时允许从 Cup Rack 选杯。
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

## Implementation Report

### Confirmed Layout Approval
Four existing cups may be staggered slightly using Position / Rotation only. No Camera, Scale or collider reshaping to solve occlusion. The first blocked layout was superseded by the user-approved staggered layout.

### Modified Files
- `Assets/Scripts/Interaction/InteractionCoordinator.cs`
- `Assets/Scripts/Interaction/CurrentDrinkRecord.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Editor/InteractionCoordinatorCupCheck.cs` + `.meta`
- This ticket.

### What Changed
- ServiceZone position: `(0.62, 0.90, 0.97)`, bringing Rack and Sink/Wash together into view.
- Four existing cup mesh centers use staggered world X/Z positions: Coupe `(0.47, 0.88)`, Highball 01 `(0.56, 0.83)`, Highball 02 `(0.67, 0.89)`, Rocks `(0.74, 0.84)`. Heights, rotations and scales remain unchanged.
- The four previously decorative cups receive standard FinalGlass DrinkContainer components and mesh-sized BoxColliders. Collider geometry is unchanged from the initial T13 setup; the overlap fix uses cup Transform offsets only. Existing DrinkContainer default capacity remains unchanged; no liquid visuals or pour flow are added here.
- ServePosition is `(-0.20, 0.925, 0.85)` with identity rotation. Each cup's actual visible bottom aligns to it, preserving model/root offsets.
- Stable + ShakeComplete selects an actual configured Rack cup. SelectedGlass stores the DrinkContainer reference identifying the chosen cup. Empty replacement returns the old cup to its cached Slot pose; any positive current volume rejects replacement without changing data.
- R / order switch return the current cup and clear SelectedGlass. Cup selection does not change preparation version, Taste or Shake facts. No Cup Flair target is added.

### Validation
- T13 Play Mode check passed: four complete visible bounds / correct center ray hits; six main Bottle, Jigger, Shaker and Ice Well clicks; all four Serve poses; state gates; empty swaps / exact old-cup return; 0.001-volume replacement rejection; R / order reset.
- Off-screen legacy test bottles remain in their original positions; tests require the six main visible bottles to remain clickable. Existing bottle lower Bounds may extend below the viewport; this ticket does not change those poses.
- Re-run with `InteractionCoordinatorCupCheck.RunBatch` from saved Edit Mode. SetupAndRun is the scene setup helper.

### Manual Check — Pending
1. Confirm all four Rack cups are visibly grouped, easy to click, and the Sink/Wash area is recognizable.
2. Close + Shake, then choose each cup. Verify visible Serve placement and empty-cup replacement.
3. Check Bottle / Jigger Measurement, Shaker and Ice Well interactions remain usable with a cup at ServePosition.
4. Press R or switch order: the cup returns to its Rack Slot.

### Known Limitations
No Serve Ice, Shaker-to-Cup Pour, Submit, Cup Flair or layout polish. Nonempty replacement is automated with injected test liquid because Pour is a later ticket. Manual visual approval remains required.
- Final verification: T10, T11 and T12 direct dependency checks PASS; no full earlier-ticket regression run. No runtime Error / Exception during successful checks. Initial layout/check failures were corrected. Renderer / Shader / Camera / Hold Anchors / Measurement UI remain unchanged.
- Final stagger refinement: Highball 02 moved another 0.02 on world X. Each of the four Rack cups passes its center plus four nearby viewport-offset rays (20/20); the final T13 rerun also checks neighboring rays at each Serve pose and PASS. No collider reshaping or Scale change was used.
