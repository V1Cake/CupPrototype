# Context Protocol

用于控制 ChatGPT 和 Codex 的上下文。

## 什么时候新开 Chatbox

出现以下情况时优先新开：

- 进入一个新的 Gameplay 模块
- 上一个模块已经 Closeout
- 开始讨论新的 Architecture
- 当前对话中包含大量旧截图、旧日志和废弃方案
- Codex 已连续完成多个独立 Ticket
- AI 开始频繁依赖很久以前的聊天内容

## ChatGPT 新窗口最小输入

提供：

1. PROJECT_STATUS.md
2. DECISION_LOG.md
3. 当前模块的 GRILL / SPEC / ARCHITECTURE
4. 必要时再提供 V0.4 策划案

不要默认复制全部历史聊天。

## Codex 新窗口最小输入

提供：

1. PROJECT_STATUS.md
2. AI_EXECUTION_RULES.md
3. 当前模块 SPEC
4. 当前模块 ARCHITECTURE
5. 当前要执行的一个 Ticket

不要把完整旧 Codex Chatbox 当成项目记忆。

## 模块完成后的写回

每个模块完成后必须更新：

- PROJECT_STATUS.md
- DECISION_LOG.md
- 当前模块 CLOSEOUT.md

只有写入这些文件的事实，才作为稳定项目上下文。

## 冲突处理

如果 AI 的说法与文档冲突：

1. 先以文档为准
2. 指出冲突
3. 由用户决定是否修改文档
4. AI 不允许静默改变项目事实
