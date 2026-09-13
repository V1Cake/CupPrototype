# T15.2 — Generic Cup Liquid Visual Foundation

> Status: Implemented / Automated PASS / Manual Pending  
> Purpose: 为 Coupe / Highball / Rocks 建立统一杯中液体视觉基础。  
> Note: T15 暂不 Close，完成本票后一起验收出杯视觉。

## Goal
让三种 Serve Cup 都能根据 `DrinkContainer` 的实际液体量显示正确液位，并支持可配置的基础液体颜色；结构需能继续扩展颜色混合、分层、泡沫、冰块等视觉效果。

## Scope
杯子液体视觉相关脚本、三个 Cup 场景实例 / Prefab 接线、必要的 `SampleScene.unity`。  
优先复用现有 `LiquidVisualController` 中可用能力；若其职责已不适合扩展，可做最小整理，但不要重写 DrinkSystem。

## Do

- **统一数据来源**
  - `DrinkContainer` 继续作为液体实际数据唯一来源。
  - 视觉组件只读取：
    - Current Volume
    - Max Volume
    - 当前液体 / Ingredient 信息
  - 视觉层不得修改 Gameplay 液体数据。

- **建立通用 Cup Liquid Visual**
  - Coupe / Highball / Rocks 使用同一套视觉脚本 / 接口。
  - 每种杯型只通过配置决定：
    - 液面最低高度
    - 液面最高高度
    - 液体 Mesh / Renderer 引用
    - 杯型所需的局部 Offset / Scale 参数
  - 不为每种杯型复制一套独立逻辑。

- **当前版本必须实现**
  - Cup 为空 → 液体视觉隐藏或液位为 0。
  - Cup 收到液体 → 液位按 `currentVolume / maxVolume` 实时变化。
  - Cup 满 → 液位达到配置的最大高度，不穿出杯体。
  - 支持一个基础可配置颜色。
  - T15 AutoPour 过程中液位应随实际转移结果更新。
  - R / 切单 / Cup Reset 后视觉同步清空。

- **扩展边界**
  - 结构上允许未来继续加入：
    - Ingredient 混合颜色
    - 多层液体
    - 透明度 / 发光
    - 泡沫 / 气泡
    - Serve Ice 可见模型
    - Garnish
  - 当前不要实现这些效果。
  - 如果需要额外数据结构，只建立最小 Visual Profile / 配置层，不建立大型 VFX Framework。

- **场景接线**
  - Coupe / Highball / Rocks 三杯都必须配置完整。
  - 检查液体 Mesh 不与杯体明显穿模。
  - 不改变三杯现有 Rack / Serve Position 交互位置。

## Do Not
- 不修改 DrinkContainer 的 Gameplay 规则。
- 不实现真实颜色混合、分层、泡沫、冰块视觉。
- 不改 Camera、Cup 交互规则、评分、Gesture、Outline / Shader 主系统。

## Acceptance
**Auto:** 编译通过；三种 Cup 均存在有效液体视觉配置；0 / 半杯 / 满杯液位映射正确；AutoPour 后视觉量与实际 Cup Volume 一致；Reset 后清空；T15 直接依赖检查 PASS。  

**Manual:** Coupe / Highball / Rocks 各倒一次，三杯均能清楚看到液面随倒酒上升；液面不明显穿杯、不漂浮；空杯无残留液体；颜色可配置；无新增 Error。

## Implementation (2026-09-12)
- Reuses LiquidVisualController and DrinkContainer's existing UpdateVisual / ClearVisual calls. Cup mode renders a closed basic liquid volume with a flat surface; the top height follows actual CurrentVolume / MaxVolume. No Gameplay data is written by the visual.
- Three scene Cup instances use the same controller, shared URP/Lit base material, and a Cup_Liquid_Surface child MeshFilter / Renderer. A small runtime mesh follows the configured width profile; it is released when the controller is destroyed.
- Coupe uses a narrowing profile sampled from the actual bowl interior; Highball and Rocks use constant interior width. Existing Cup root / Rack / Serve transforms, colliders and Camera remain unchanged.
- Existing tool liquid mode remains unchanged. No changes to DrinkContainer, Ingredient mixing, scoring, Gesture, Outline or Shader code.
- Mesh/Renderer references and per-Cup geometry/color configuration remain separate from Gameplay. No extra VFX framework, layering, foam, ice or garnish is implemented.

## Inspector Configuration
Select the Cup root → LiquidVisualController → Cup Surface:
- Use Cup Surface: enabled on all three Cups.
- Surface Min Height / Surface Max Height: local bottom and full liquid height.
- Surface Width By Fill: width multiplier over normalized fill, used for the Coupe bowl shape.
- Base Color: the configurable Cup liquid color (independent of ingredient color in this ticket).
- Cup_Liquid_Surface local X/Z offset and scale define the interior center and width. Do not move the Cup root to adjust liquid geometry.

| Cup | Min local height | Max local height | Full width |
| --- | --- | --- | --- |
| Coupe | 0.137 | 0.178 | 0.0596 |
| Highball | 0.072 | 0.192 | 0.0485 |
| Rocks | 0.073 | 0.147 | 0.0585 |

## Modified Files
- Assets/Scripts/DrinkSystem/LiquidVisualController.cs
- Assets/Scenes/SampleScene.unity
- Assets/Art/Glassware/Common/Materials/M_CupLiquid_Base.mat + .meta
- Assets/Editor/CupLiquidVisualCheck.cs + .meta
- Assets/Editor/InteractionAutoPourCheck.cs
- This ticket.

## Automated Check — PASS
- CupLiquidVisualCheck.SetupAndRun: Unity compilation, three valid configurations, 0 / half / full levels, configured base color and R cleanup passed.
- InteractionAutoPourCheck.RunBatch: T15.1 + T15 passed. Extended to all three Cup types, checking visual height against actual volume during Pour, after completion and after R/order/cancel cleanup.
- Main Camera half/full screenshots for all three Cups inspected; basic liquid volume is visible inside the cups without an obvious floating disk or rim overflow. Screenshots are temporary test artifacts, not project assets.
- No test runtime Error / Exception. No missing LiquidVisualController warnings for the three configured Cups in the final AutoPour log. Existing Unity obsolete API warnings remain.
- Logs: %TEMP%/CupPrototype-T15-2-volume.log and %TEMP%/CupPrototype-T15-2-autopour.log.

## Manual Check — Pending
1. Pour into Coupe, Highball and Rocks separately; inspect liquid rise, interior fit and full height.
2. Press R and switch order; confirm each empty Cup hides liquid.
3. Change Base Color on the Cup controller before Play and repeat Pour.
4. Review T15/T15.1 Serve Held and Pour presentation together. T15 remains open; no Full Regression performed.
