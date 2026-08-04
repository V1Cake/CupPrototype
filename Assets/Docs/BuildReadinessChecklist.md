# Build Readiness Checklist

## Required Scene Objects

- Main Camera：恰好一个，Tag 为 `MainCamera`。
- EventSystem：恰好一个。
- GameManager：恰好一个；挂载 `DrinkTestManager`、`DemoRoundManager`、`GestureTemplateLibrary`、`DemoModeController` 和 `BuildReadinessValidator`。
- 调酒对象：六个材料瓶、`Jigger_Test`、`Shaker_Test`、`Cup_Test`。
- 环境对象：`Directional Light`、`Global Volume`、`BarTable`。
- UI：Order、Score、Round Status、Hint、Message 的 TMP 引用均已绑定。

## Player Mode Visible Objects

- `DemoModeController.playerVisibleObjects` 应覆盖 Order、Score、Round Status、Hint 和 Message。
- Order 与 Score 不得位于 `developerOnlyObjects`（例如整个 DebugPanel）层级下。
- Player 模式只保留玩法所需提示；开发者录制、诊断面板和内部日志入口应隐藏。

## Developer Only Objects

- `DebugPanel`、手势模板录制 UI、内部状态文本等加入 `developerOnlyObjects`。
- 不要把 GameManager、DemoModeController、EventSystem 或玩家必须看到的 UI 加入该列表。
- 按 F1 往返切换 Player / Developer，确认对象只改变可见性，不重置饮品、评分或目标。

## Required Data Assets

- `DemoRoundManager.availableTargets` 至少包含一个非空 `TargetDrinkData`；演示配置应按顺序包含三个目标饮品。
- `GestureTemplateLibrary.templateAssets` 至少包含一个 `templateId` 非空且有标准化轨迹点的模板。
- `DrinkTestManager.scoringContainer` 明确指向 `Cup_Test` 的 `FinalGlass`。

## Required Script References

- `DemoRoundManager`：Order Display、Drink Test Manager、Round Status Text。
- `DrinkTestManager`：评分杯及现有 UI/评分引用。
- `DemoModeController`：Player Visible Objects、Developer Only Objects。
- Order、Score、Hint、Message 各控制器的 TMP 字段不可为空。

## Build Settings

- 将正式 Demo 场景加入 Build Settings，并确认它是预期启动场景。
- Build 前清空 Console，运行 `BuildReadinessValidator.ValidateBuildReadiness()`，处理所有 Warning。
- `UnityEditor` 引用只能位于 `Editor` 文件夹或 `#if UNITY_EDITOR` 内；不要从运行时程序集直接依赖 Editor 类。
- `GestureTemplateRecorder.saveRecordedTemplateAsAsset` 仅用于 Editor，正式 Player 不保存模板资产。

## Final Playtest Steps

1. Player 模式启动，确认无重复 Camera、EventSystem、GameManager 或 DemoModeController Warning。
2. 用 1、2、3 与 N 切换三份订单，名称、配方和评分目标同步。
3. 验证容器规则：Bottle → Jigger、Jigger → Shaker、Shaker → Cup；Bottle 不直倒 Shaker，Shaker 不倒 Jigger，Cup 不向任何容器倒液。
4. 完成一次倒料、摇匀、入杯、F 评分、R 重置流程。
5. 切到 Developer 模式测试录制/诊断，再切回 Player，确认玩家 UI 仍可见且状态未被清空。
6. 创建 Development Build 和正式 Build 各一次，确认没有运行时 `UnityEditor` 依赖或持续刷屏日志。
