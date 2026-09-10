# Decision Log

## D-001 开发流程

采用：

GRILL
→ SPEC
→ ARCHITECTURE
→ TICKETS
→ IMPLEMENT
→ TEST
→ CLOSEOUT

不再默认用一条长 Codex 指令完成整个模块。

---

## D-002 Codex 工作范围

Codex 默认只执行当前 Ticket。

禁止：

- 顺手加功能
- 无关重构
- 自行扩需求
- 自行加入未来功能
- 用大量代码注释解释设计

---

## D-003 项目记忆

项目文档是稳定 Source of Truth。

ChatGPT 和 Codex 的旧聊天记录只作为辅助上下文。

---

## D-004 新 Chatbox

进入新的 Gameplay / Architecture 模块时，优先新开 ChatGPT 和 Codex Chatbox。

---

## D-005 P5-3 Selection Visual

Selection 使用风格化 Outline：

- 浅青色主轮廓
- 外侧深色细边
- 不进入 Bloom
- 不修改物体原材质
- 不改变透明物体表现
- Boston Shaker 按整体轮廓显示
- Hover 当前不实现

---

## D-006 Jigger Measurement 顺序

不直接开始 P5-4 编码。

必须先完成：

Interaction Core Re-Baseline

之后再进行：

Research
→ Spec
→ Architecture
→ Tickets
→ Implementation

---

## D-007 P5-3 Closeout（2026-09-08）

P5-3 Selection Outline：PASS / Frozen Prototype。

Implementation PASS、Visual PASS；用户确认人工 Functional T1-T8 PASS，无新增 Error / NullReference，已知旧 Warning 不属于 P5-3 阻塞项。

P5-3 已冻结，后续除明确 Ticket 外不修改其行为。下一正式模块保持 Interaction Core Re-Baseline。
