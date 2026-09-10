# T07 — Process Ice in Preparing Shaker

> Status: Functional Closeout Pending — Automated PASS / Manual Pending

## Goal
实现 Shaker 在 `Preparing` 状态下的一次性 Process Ice 操作，并记录制作事实。

## Scope
`InteractionCoordinator.cs`、`ShakerPreparation.cs`、必要的 CurrentDrinkRecord / IceWell 最小实现、`SampleScene.unity`。

## Do
- 当 Shaker=`Preparing`、`ActionState=Stable` 时，玩家点击 Ice Well：
  - 自动执行一次标准 Process Ice 动作；
  - Shaker 记录“本轮已加入 Process Ice”；
  - CurrentDrinkRecord 同步记录 `ProcessIce=true`；
  - 动作结束后 `ActionState → Stable`。
- Process Ice 只允许一次：
  - 同一制作尝试中再次点击 Ice Well，不重复增加冰、不重复改写数据；
  - 可给简短反馈，但不报错、不改变液体组成。
- Process Ice 与 Serve Ice 分开记录；本 Ticket 只处理 Shaker 内的 Process Ice。
- Process Ice 当前只作为制作过程事实：
  - 不直接修改 Sour / Sweet / Bitter / Fresh / Body / Aroma；
  - 不在 Interaction 中硬编码评分惩罚或奖励。
- 若当前还没有 `CurrentDrinkRecord`，只建立满足本 Ticket 的最小数据对象/字段，不扩展完整 OrderSystem。
- Shaker 非 Preparing、系统 Busy 时点击 Ice Well，应在状态变化前拒绝。

## Do Not
- 不做冰量/冰型选择。
- 不做 Serve Ice、Close Shaker、Shake、Taste。
- 不改 Flavor、评分、Gesture、P5-3 Outline。

## Acceptance
**Auto:** 编译通过；Preparing 下首次点击只记录一次 Process Ice；重复点击不叠加；非法状态无变化；CurrentDrinkRecord 与 Shaker 事实一致；T03–T06 回归 PASS。  

**Manual:** 点击 Ice Well 后有明确可理解的加冰表现/反馈；重复点击不会再加；Bottle/Jigger/Measurement/Transfer 流程无新增退化。

## Implementation Check — 2026-09-10

- 新增最小 `CurrentDrinkRecord`，仅保存一次性 `ProcessIce`。Shaker 的 ProcessIce 属性读取绑定的同一份记录，避免重复数据不同步；没有新增 Serve Ice 或 OrderSystem。
- Coordinator 识别绑定 IceWell 的 Collider，只在 Stable 且 Shaker Preparing 时调用一次记录。即时完成标准加冰事实，并显示固定屏幕 `PROCESS ICE ADDED TO SHAKER` 反馈；不新增动画等待状态，完成后保持 Stable。
- 同一制作尝试的重复点击返回失败，不再记录、不改变任何容器的容量、材料、颜色、Flavor。Busy、Rest、错误目标均在写入前拒绝。
- SampleScene 为现有 `Bar_Art_P3/IceWell` 添加覆盖冰槽的点击 Collider，绑定 Coordinator、CurrentDrinkRecord、Shaker 及最小反馈面板。反馈不拦截鼠标。
- 单独调用 Shaker ResetToRest 不清除制作记录，符合 Architecture。R 的 ResetCurrentDrink 则通过 ResetPreparation / ResetAttempt 同时清除 ProcessIce、刷新 UI 并复用 ResetToRest；未实现新订单 / Remake 流程。
- Automated Check：PASS。Unity 编译通过；首次 / 重复记录、非法状态、Shaker / Record 一致、Ice Well 摄像机射线可达、反馈可见、所有液体数据不变及记录在 Shaker 回 Rest 后保留均通过。
- T03–T06 回归全部 PASS，包括已有 ProcessIce 时原 Primary Jigger Transfer 及连续三轮双持 Measurement → Transfer，ProcessIce 未丢失。运行 Console 无 Error / Exception，旧场景配置警告保留。
- 可重复检查：新 Play Mode、空手、Shaker Rest 且尚未加冰时，执行 `Tools/CupPrototype/Validate T07 Process Ice (Play Mode)`；输出 `T07_AUTOMATED_PASS`。
- 已检查 Game View 反馈截图；实际点击与反馈可理解性仍待用户 Manual Check。状态为 Functional Closeout Pending，不开始下一 Ticket。
- Flavor、评分、Gesture、Renderer / Shader 保持不变；DrinkTestManager 仅在原 ResetCurrentDrink 入口接入制作状态重置。

## R Reset Correction

- 原则：**Gameplay 数据决定 UI，UI 不单独保存“有没有冰”的状态。** `CurrentDrinkRecord.ProcessIce` 是唯一事实来源。
- 加冰与 ResetAttempt 均发出同一 Changed 通知；Shaker 统一调用 RefreshProcessIceFeedback，true 显示 `PROCESS ICE ADDED TO SHAKER`，false 隐藏。初始化也执行同一刷新逻辑。
- R → ClearCurrentDrink → ResetCurrentDrink → Coordinator.ResetPreparation → Shaker.ResetAttempt：恢复 Rest，并清除记录中的 ProcessIce；Shaker 的 ProcessIce 读取同一记录，因此同步为 false。
- 后续 Remake 可复用该重置 / 刷新入口，本次没有实现 Remake 或 OrderSystem，也没有更改 Ice 评分或 Flavor。
- 自动检查已覆盖实际 R 使用的 ClearCurrentDrink 调用链：加冰 true / 文字显示 → R false / 文字隐藏 / 精确 Rest → 重新 Acquire 到 Preparing → 再次加冰成功，重复点击仍拒绝；直接重置记录也通过同一逻辑隐藏文字。
- R Reset Correction Automated Check：PASS。独占 Unity 批处理输出 `T03_T07_RESET_REGRESSION_PASS`，T03、T04、T05、T06、T07 全部通过；无新增运行 Error / Exception。保留原有场景配置及已弃用 API 警告。
- 可重跑完整回归：Unity 批处理执行 `InteractionCoordinatorIceCheck.RunBatch`；该入口加载 SampleScene、进入 Play Mode、顺序运行 T03–T07，完成后自动退出，不保存 Scene。
- Manual 待确认：加冰文字出现，按 R 立即消失；重新取得 Shaker 后可正常再加一次 Process Ice。
