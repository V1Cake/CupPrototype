# AI Execution Rules

本文件用于约束 Codex。

## 强制规则

1. 只实现当前 Ticket。
2. Ticket 未写的功能不要新增。
3. 不做顺手优化。
4. 不做无关重构。
5. 不修改已通过模块的行为，除非 Ticket 明确要求。
6. 不自行把 Debug 功能升级成正式玩法。
7. 不在代码里写长篇解释。
8. 注释只解释必要的非显然逻辑。
9. 优先最小改动。
10. 修改前先确认受影响范围。
11. 如果发现需求与现有代码冲突，停止并报告。
12. 如果需要新增第三方包、Renderer Feature、全局输入方案或核心数据结构，先停止并请求确认。
13. 不创建“以后可能有用”的抽象层。
14. 不自行增加教程、提示、特效、声音、动画和设置项。
15. 不用“看起来正常”代替测试。

## 每个 Ticket 完成后的输出格式

只汇报：

### Modified Files

实际修改文件。

### What Changed

完成了什么。

### What Did Not Change

明确哪些旧系统没有修改。

### Manual Test Steps

给用户实际测试步骤。

### Known Limitations

当前限制。

### Errors / Warnings

是否存在 Error 或 Warning。

避免长篇总结。
