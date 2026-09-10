# CupPrototype Project Status

Baseline: V0.4
Last Update: 2026-09-08

## 1. 已验证底层

- Ingredient / DrinkContainer / Flavor 六维数据：PASS
- Bottle → Jigger：PASS
- Jigger → Shaker：PASS
- Shaker → Cup：PASS
- 非法转移限制：PASS
- Target / Score 技术底层：PASS
- Gesture / Flair：已有技术 Prototype

## 2. 工作台与场景

- 第一人称工作台布局：Prototype 已建立
- Speed Rail：已建立
- Build Zone：已建立
- Ice Bin：已建立
- Garnish 区：已建立
- Boston Shaker：PASS
- 吧台工具模型：Prototype 已建立
- Lighting / Mood：First Pass

## 3. P5 状态

### P5-1

PASS。

### P5-2A BAR ORDER DISPLAY

PASS Prototype。

当前显示器已经能够在正常 Direct View 下读取订单基础信息。

### P5-2B Recipe Library

PASS Prototype。

已验证：

- A-Z
- Taste
- Style
- Method
- Recipe List
- Recipe Detail
- Ingredient / Amount
- 六维 Taste Target
- Preparation Detail

正式 UI 与美术后续再做。

### P5-3 Selection Outline

PASS / Frozen Prototype

当前实现：

- IDLE：无轮廓
- SELECTED：浅青色轮廓
- HELD：沿用轮廓，并读取 DragController Held 状态
- Hover：暂未实现
- 主轮廓约 2 px
- 外侧约 1 px 深色对比边
- Color RGBA：(0.58, 0.82, 0.86, 1)
- 不参与 Bloom
- 不修改物体原材质
- 不修改透明度
- 不修改液体颜色
- Boston Shaker 作为整体显示轮廓

当前状态：

- Implementation PASS
- Visual PASS
- Functional T1-T8 PASS（用户于 2026-09-08 确认人工测试全部通过）
- 无新增 Error / NullReference
- 已知旧 Warning 不属于 P5-3 阻塞项

P5-3 已冻结，后续除明确 Ticket 外不修改其行为。

## 4. 下一模块

下一正式模块：

Interaction Core Re-Baseline

在 Interaction Core 确认前，不直接开发：

- 正式手部系统
- Jigger 最终精确控量
- 新 Pour 操作
- 新 Shake 操作
- 新 Stir 操作
- 大规模 Input 重构

## 5. 下一顺序

1. Interaction Core GRILL
2. Interaction Core SPEC
3. Interaction Core ARCHITECTURE
4. 拆 TICKETS
5. Codex 分 Ticket 实现
6. 人工测试
7. CLOSEOUT
8. 再进入 P5-4 Jigger Measurement
