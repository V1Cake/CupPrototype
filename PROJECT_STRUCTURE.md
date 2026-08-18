# CupPrototype 项目目录与文件说明

> 快照日期：2026-08-17  
> 项目类型：Unity 6（`6000.4.11f1`）调酒玩法原型  
> 说明：本文档记录当前工作区，而不只是 Git 已提交内容。

## 阅读约定

- Unity 为 `Assets/` 中几乎每个文件和目录生成同名 `.meta` 文件；它们保存 GUID、导入器和资源引用信息。下文不重复列出每个 `.meta`，但所有 `.meta` 的作用均相同，必须随对应资源一起移动或提交。
- `Library/`、`Temp/`、`Logs/`、`UserSettings/` 是 Unity/本机生成目录，内容会变化且通常不提交，因此只说明目录用途，不逐文件展开。
- `.csproj`、`.slnx` 和 `Tools/Blender/__pycache__/` 同样属于工具生成内容，可由 Unity、IDE 或 Python 重建。
- 同一行列出多个文件时，说明适用于该行中的每一个文件。

## 目录总览

```text
CupPrototype/
├─ .agents/                         # 当前为空，预留给代理/自动化配置
├─ .vscode/                         # VS Code 工作区配置
├─ Assets/                          # Unity 项目资源根目录
│  ├─ Art/                          # 酒瓶、杯具、吧台工具的模型、预制体和验收截图
│  │  ├─ Bottles/                   # 六类基酒瓶资源
│  │  ├─ Glassware/                 # Coupe、Highball、Rocks 杯具资源
│  │  └─ Tools/                     # Boston Shaker、Jigger 工具资源
│  ├─ Data/                         # ScriptableObject 游戏数据
│  │  ├─ GestureTemplates/          # 花式动作模板
│  │  ├─ Ingredients/               # 原料与基酒数据
│  │  └─ TargetDrinks/              # 目标饮品/配方数据
│  ├─ Docs/                         # 项目流程与验收文档
│  ├─ Materials/                    # 原型场景通用材质
│  ├─ Prefabs/                      # 项目预制体及制作说明
│  ├─ Scenes/                       # 主场景与美术预览场景
│  ├─ Scripts/                      # 运行时代码与编辑器代码
│  │  ├─ Core/                      # 当前为空，预留公共核心代码
│  │  ├─ DrinkSystem/               # 容器、倒酒、摇酒、评分、味觉反馈
│  │  ├─ Flair/                     # 花式动作录制、模板、识别、播放与调试
│  │  ├─ Game/                      # 演示模式和回合流程
│  │  ├─ Interaction/               # 拖拽、选择、高亮、倾倒反馈
│  │  ├─ Scoring/                   # 评分文本格式化
│  │  ├─ UI/                        # 订单、提示、消息和调试界面
│  │  ├─ Validation/                # 场景与构建就绪检查
│  │  └─ Visual/                    # 酒瓶外观控制
│  ├─ Settings/                     # URP、渲染器和后处理配置
│  ├─ TextMesh Pro/                 # TextMesh Pro 默认字体、材质和 Shader
│  └─ TutorialInfo/                 # Unity 模板自带的欢迎页资源
├─ Library/                         # Unity 导入缓存，可重建
├─ Logs/                            # Unity/包工具日志，可清理后重建
├─ Packages/                        # Unity 包依赖声明与锁定版本
├─ ProjectSettings/                 # Unity 项目级设置
├─ Temp/                            # Unity 临时文件，可重建
├─ Tools/Blender/                   # Blender 美术资源生成脚本
├─ UserSettings/                    # 本机 Unity 编辑器偏好
├─ .gitignore                       # Git 忽略规则
├─ Assembly-CSharp*.csproj          # Unity 生成的 C# 工程文件
├─ CupPrototype.slnx                # Unity/IDE 解决方案入口
├─ sets Packages ProjectSettings .gitignore
│                                      # 疑似误保存的 Git diff/stat 输出，不参与 Unity
└─ PROJECT_STRUCTURE.md             # 本文档
```

## 根目录与开发工具配置

| 文件 | 功能 |
|---|---|
| `.gitignore` | 忽略 Unity 缓存、构建产物、本机设置和 IDE 生成文件。 |
| `Assembly-CSharp.csproj` | Unity 生成的运行时 C# 工程描述，供 IDE 索引和编译提示使用。 |
| `Assembly-CSharp-Editor.csproj` | Unity 生成的仅编辑器 C# 工程描述。 |
| `CupPrototype.slnx` | 新式 .NET 解决方案文件，聚合 Unity 生成的 C# 工程。 |
| `sets Packages ProjectSettings .gitignore` | 内容是一次 Git 变更统计/警告文本的残留，不是 Unity 配置或源码；可在确认无用后另行删除。 |
| `.vscode/extensions.json` | 推荐安装 Visual Studio Tools for Unity 扩展。 |
| `.vscode/launch.json` | 配置“Attach to Unity”调试入口。 |
| `.vscode/settings.json` | 隐藏 Unity 生成资源、关联 YAML 文件类型、启用解决方案文件嵌套。 |

## Assets 根文件

| 文件 | 功能 |
|---|---|
| `Assets/InputSystem_Actions.inputactions` | Unity Input System 的默认输入动作资产。 |
| `Assets/Readme.asset` | Unity 模板欢迎页的数据资产。 |
| `Assets/testScript.cs` | 模板/试验用空 MonoBehaviour，仅保留 `Start` 和 `Update` 骨架。 |

## 美术资源 `Assets/Art`

### 酒瓶 `Assets/Art/Bottles`

| 文件 | 功能 |
|---|---|
| `Brandy/Model/Bottle_Brandy.fbx` | 白兰地酒瓶三维模型。 |
| `Gin/Model/Bottle_Gin.fbx` | 金酒酒瓶三维模型。 |
| `Rum/Model/Bottle_Rum.fbx` | 朗姆酒瓶三维模型。 |
| `Tequila/Model/Bottle_Tequila.fbx` | 龙舌兰酒瓶三维模型。 |
| `Vodka/Model/Bottle_Vodka.fbx` | 伏特加酒瓶三维模型。 |
| `Whiskey/Model/Bottle_Whiskey.fbx` | 威士忌酒瓶三维模型。 |
| `Common/Materials/M_BottleGlass_Preview.mat` | 酒瓶玻璃预览材质。 |
| `Common/Materials/M_Cap_Black_Preview.mat` | 黑色瓶盖预览材质。 |
| `Common/Materials/M_Liquid_Amber_Preview.mat` | 琥珀色酒液预览材质。 |
| `Common/Materials/M_Liquid_AmberDark_Preview.mat` | 深琥珀色酒液预览材质。 |
| `Common/Materials/M_Liquid_AmberLight_Preview.mat` | 浅琥珀色酒液预览材质。 |
| `Common/Materials/M_Liquid_Clear_Preview.mat` | 透明酒液预览材质。 |
| `Common/Materials/M_Liquid_Cool_Preview.mat` | 冷色酒液预览材质。 |
| `Common/Materials/M_Liquid_WarmGold_Preview.mat` | 暖金色酒液预览材质。 |
| `Common/Materials/M_PreviewGround.mat` | 美术预览场景的地面材质。 |
| `Prefabs/VFX_Bottle_Brandy.prefab` | 配好模型、材质、碰撞/交互组件的白兰地酒瓶预制体。 |
| `Prefabs/VFX_Bottle_Gin.prefab` | 金酒酒瓶预制体。 |
| `Prefabs/VFX_Bottle_Rum.prefab` | 朗姆酒瓶预制体。 |
| `Prefabs/VFX_Bottle_Tequila.prefab` | 龙舌兰酒瓶预制体。 |
| `Prefabs/VFX_Bottle_Vodka.prefab` | 伏特加酒瓶预制体。 |
| `Prefabs/VFX_Bottle_Whiskey.prefab` | 威士忌酒瓶预制体。 |
| `Validation/ArtP2_2_SixBottles_Front.png`<br>`Validation/ArtP2_2_SixBottles_ThreeQuarter.png` | 六种酒瓶的正面与三分之四视角验收图。 |
| `Validation/ArtP2_3B_A_SixSpirits_Scene.png`<br>`Validation/ArtP2_3B_B_GameView.png`<br>`Validation/ArtP2_3B_C_Gin_Highlight.png`<br>`Validation/ArtP2_3B_D_Whiskey_Tilt_18deg.png` | 酒瓶场景、游戏视图、高亮和倾斜效果验收图。 |
| `Validation/ArtP2_4_A_Collider_Front.png`<br>`Validation/ArtP2_4_B_GameView_CompactLayout.png`<br>`Validation/ArtP2_4_C_RaycastQA.png`<br>`Validation/ArtP2_4_D_TiltQA.png` | 碰撞体、紧凑布局、射线选择和倾斜交互 QA 截图。 |
| `Validation/ArtP2_5_A_SampleScene_Clean.png` | 主场景清理后的整体美术验收图。 |
| `Vodka/Validation/Vodka_EditMode.png`<br>`Vodka/Validation/Vodka_EditMode_Close.png`<br>`Vodka/Validation/Vodka_PlayMode_Normal.png`<br>`Vodka/Validation/Vodka_PlayMode_Tilt_Close.png` | 伏特加模型在编辑/运行、普通/倾斜状态下的专项验收图。 |

### 杯具 `Assets/Art/Glassware`

| 文件 | 功能 |
|---|---|
| `Coupe/Model/Glass_Coupe.fbx` | Coupe 高脚浅碟杯三维模型。 |
| `Highball/Model/Glass_Highball.fbx` | Highball 高球杯三维模型。 |
| `Rocks/Model/Glass_Rocks.fbx` | Rocks 古典杯三维模型。 |
| `Common/Materials/M_Glassware_Preview.mat` | 杯具共用的玻璃预览材质。 |
| `Prefabs/VFX_Glass_Coupe.prefab` | Coupe 杯的 Unity 预制体。 |
| `Prefabs/VFX_Glass_Highball.prefab` | Highball 杯的 Unity 预制体。 |
| `Prefabs/VFX_Glass_Rocks.prefab` | Rocks 杯的 Unity 预制体。 |
| `Validation/ArtP3_3_A_GlassSet_Blender.png`<br>`Validation/ArtP3_3_B_GlassSet_UnityPreview.png` | 杯具在 Blender 与 Unity 中的一致性验收图。 |
| `Validation/ArtP3_3_C_Rocks_GameView.png`<br>`Validation/ArtP3_3_D_Rocks_Liquid.png`<br>`Validation/ArtP3_3_E_Rocks_Collider.png`<br>`Validation/ArtP3_3_F_Rocks_Tilt.png` | Rocks 杯的游戏视图、酒液、碰撞体和倾斜效果验收图。 |

### 吧台工具 `Assets/Art/Tools`

| 文件 | 功能 |
|---|---|
| `BostonShaker/Model/BostonShaker.fbx` | 波士顿摇酒壶三维模型。 |
| `BostonShaker/Prefabs/VFX_BostonShaker.prefab` | 摇酒壶的 Unity 预制体。 |
| `BostonShaker/Validation/ArtP3_2_A_BostonShaker_Blender.png`<br>`ArtP3_2_B_BostonShaker_UnityScene.png`<br>`ArtP3_2_C_BostonShaker_GameView.png`<br>`ArtP3_2_D_BostonShaker_Tilt.png`<br>`ArtP3_2_E_BostonShaker_Collider.png` | 摇酒壶模型、Unity、游戏、倾斜和碰撞体的验收截图。 |
| `Jigger/Model/Jigger_Japanese.fbx` | 日式量酒器三维模型。 |
| `Jigger/Prefabs/VFX_Jigger_Japanese.prefab` | 日式量酒器的 Unity 预制体。 |
| `Jigger/Validation/ArtP3_1_A_Jigger_Blender.png`<br>`ArtP3_1_B_Jigger_UnityScene.png`<br>`ArtP3_1_C_Jigger_GameView.png`<br>`ArtP3_1_D_Jigger_Tilt.png`<br>`ArtP3_1_E_Jigger_Collider.png` | 量酒器模型、Unity、游戏、倾斜和碰撞体的验收截图。 |
| `Common/Materials/M_BarTool_Metal_Preview.mat` | 吧台金属工具共用的预览材质。 |

## 游戏数据 `Assets/Data`

### 手势模板

| 文件 | 功能 |
|---|---|
| `GestureTemplates/BottleLoop_01.asset` | 酒瓶绕环动作的归一化轨迹模板。 |
| `GestureTemplates/CupSwirl_01.asset` | 杯中旋转动作模板。 |
| `GestureTemplates/DevTemplate_0.asset` | 开发调试用手势模板。 |
| `GestureTemplates/JiggerFlip_01.asset` | 量酒器翻转动作模板。 |
| `GestureTemplates/ShakerRoll_01.asset` | 摇酒壶滚动动作模板。 |

### 原料与目标饮品

| 文件 | 功能 |
|---|---|
| `Ingredients/AromaBase_Test.asset` | 测试用芳香基底原料数据。 |
| `Ingredients/Bitter_Test.asset` | 测试用苦味原料数据。 |
| `Ingredients/FreshHerb_Test.asset` | 测试用新鲜草本原料数据。 |
| `Ingredients/Lemon_Test.asset` | 测试用柠檬原料数据。 |
| `Ingredients/Syrup_Test.asset` | 测试用糖浆原料数据。 |
| `Ingredients/Brandy.asset` | 白兰地的名称、颜色和风味等 `IngredientData`。 |
| `Ingredients/Gin.asset` | 金酒原料数据。 |
| `Ingredients/Rum.asset` | 朗姆酒原料数据。 |
| `Ingredients/Tequila.asset` | 龙舌兰原料数据。 |
| `Ingredients/Vodka.asset` | 伏特加原料数据。 |
| `Ingredients/Whiskey.asset` | 威士忌原料数据。 |
| `TargetDrinks/AromaBody_Test.asset` | 芳香/酒体方向的测试目标配方。 |
| `TargetDrinks/FreshBitter_Test.asset` | 清新/苦味方向的测试目标配方。 |
| `TargetDrinks/LemonSweet_Test.asset` | 柠檬/甜味方向的测试目标配方。 |

## 项目文档、材质、预制体与场景

| 文件 | 功能 |
|---|---|
| `Assets/Docs/BuildReadinessChecklist.md` | 构建前检查场景对象、数据引用、UI、Build Settings 和最终试玩步骤。 |
| `Assets/Docs/DemoRegressionChecklist.md` | Demo 功能回归测试清单。 |
| `Assets/Docs/FlairActionBookConfig.md` | 花式工具动作表的类型、优先级和推荐配置说明。 |
| `Assets/Docs/GestureTemplateNaming.md` | 手势模板 ID 和资源文件命名规则。 |
| `Assets/Docs/ToolPrefabWorkflow.md` | 吧台工具预制体的制作、组件配置和验证流程。 |
| `Assets/Materials/Cup_Glass_Test.mat` | 原型杯体玻璃材质。 |
| `Assets/Materials/Label_Base.mat` | 酒瓶标签基础材质。 |
| `Assets/Materials/Liquid_Test.mat` | 原型酒液材质。 |
| `Assets/Prefabs/Bottles/README_BottlePrefabWorkflow.md` | 酒瓶预制体的创建与配置流程。 |
| `Assets/Scenes/SampleScene.unity` | 主试玩场景，组合调酒交互、回合、评分、UI 和调试对象。 |
| `Assets/Scenes/ArtPreview_Bottles.unity` | 六类酒瓶的美术与交互预览场景。 |
| `Assets/Scenes/ArtPreview_Glassware.unity` | 三类杯具的美术预览场景。 |

## 源码 `Assets/Scripts`

### 调酒系统 `DrinkSystem`

| 文件 | 功能 |
|---|---|
| `DrinkContainer.cs` | 管理杯、量酒器和摇酒壶中的原料、容量、混合状态、容器间转移及液体显示更新。 |
| `DrinkScoreResult.cs` | 保存一次评分的总分、分项得分和反馈结果。 |
| `DrinkScoreSystem.cs` | 将当前饮品与目标配方比较，计算风味、原料和制作方法得分。 |
| `DrinkTestManager.cs` | Demo 调酒流程的主要输入协调器，处理键鼠选择、倒酒、转移、清空、评分和调试快捷键。 |
| `FlavorProfile.cs` | 表示风味维度并提供加权累加、平均和调试输出。 |
| `IngredientData.cs` | 定义原料的 ScriptableObject 数据，如名称、显示颜色和风味属性。 |
| `LiquidVisualController.cs` | 根据容器容量更新液面高度、显隐与颜色。 |
| `PourableIngredient.cs` | 标记可倒出的原料来源，并关联其 `IngredientData`。 |
| `ShakerController.cs` | 检测摇酒壶运动、累计摇晃程度并同步容器混合状态。 |
| `TargetDrinkData.cs` | 定义目标饮品的配方原料、风味目标和制作方法。 |
| `TasteFeedbackSystem.cs` | 根据风味偏差生成面向玩家的味觉反馈文本。 |

### 花式动作 `Flair`

| 文件 | 功能 |
|---|---|
| `FlairableTool.cs` | 把场景工具声明为可执行花式动作的对象，并校验/查询该工具的动作表。 |
| `FlairActionDefinition.cs` | 定义手势到花式动画、阈值和工具类型的映射配置。 |
| `FlairGestureController.cs` | 协调轨迹录制、动作识别、花式播放及播放期间的交互状态。 |
| `FlairGestureDebugInfo.cs` | 保存一次识别过程的调试指标与候选结果。 |
| `FlairGestureDebugPanel.cs` | 在 UI 中显示手势识别结果和调试信息。 |
| `FlairGestureRecognizer.cs` | 将输入轨迹与模板库比较并返回最佳匹配及详细评分。 |
| `FlairGestureType.cs` | 定义支持的花式手势枚举。 |
| `FlairToolType.cs` | 定义酒瓶、杯、量酒器、摇酒壶等工具类型枚举。 |
| `GestureMatchResult.cs` | 表示单次手势模板匹配结果。 |
| `GestureTemplateAsset.cs` | Unity 可序列化手势模板资产，并可转换为运行时数据。 |
| `GestureTemplateData.cs` | 保存运行时手势模板 ID、类型和归一化轨迹点。 |
| `GestureTemplateLibrary.cs` | 加载、查询、添加、清空并校验手势模板集合。 |
| `GestureTemplateRecorder.cs` | 采样工具移动轨迹、完成录制，并在编辑器中选择性保存模板资产。 |
| `GestureTemplateUtility.cs` | 清洗和归一化轨迹点，计算模板比较所需的距离数据。 |
| `Editor/GestureTemplateAssetSaver.cs` | 编辑器专用：创建目录、清理文件名并保存录制出的模板资产。 |

### 游戏流程、交互、UI 与其他模块

| 文件 | 功能 |
|---|---|
| `Game/DemoModeController.cs` | 在 Player/Developer 模式之间切换，并控制玩家 UI 与开发调试对象的可见性。 |
| `Game/DemoRoundManager.cs` | 管理目标饮品选择、回合开始/混合/提交/重置状态和回合状态文本。 |
| `Interaction/DragController.cs` | 使用鼠标射线选择并在拖拽平面上移动可交互对象。 |
| `Interaction/InteractableObject.cs` | 提供可拖拽/可选择对象的基础标记与交互属性。 |
| `Interaction/PourTiltFeedback.cs` | 在倒酒操作期间平滑倾斜来源容器并在结束后复位。 |
| `Interaction/SelectionHighlight.cs` | 查找目标 Renderer，并用轮廓/材质属性显示选择高亮。 |
| `Scoring/DrinkScoreFeedbackFormatter.cs` | 把评分结果格式化为分数档位和玩家反馈文本。 |
| `UI/DemoHintPanelController.cs` | 显示、隐藏或切换玩法提示面板，并构建快捷键提示。 |
| `UI/DemoMessagePanel.cs` | 订阅消息事件并显示可自动清除的临时通知。 |
| `UI/DrinkDebugUI.cs` | 显示当前原料、容量、味觉反馈和评分等调试信息。 |
| `UI/OrderDisplayController.cs` | 将当前目标饮品及其原料需求格式化为订单文本。 |
| `Validation/BuildReadinessValidator.cs` | 只读检查主场景的相机、EventSystem、管理器、容器、数据、UI 和编辑器隔离配置。 |
| `Validation/DemoSetupValidator.cs` | 运行时检查花式工具、容器与模板库的 Demo 场景配置并给出警告。 |
| `Visual/BottleVisualController.cs` | 从原料数据读取颜色，通过 `MaterialPropertyBlock` 设置酒瓶标签和可选瓶盖颜色。 |

## 渲染设置 `Assets/Settings`

| 文件 | 功能 |
|---|---|
| `DefaultVolumeProfile.asset` | 项目默认的后处理 Volume Profile。 |
| `SampleSceneProfile.asset` | `SampleScene` 使用的后处理配置。 |
| `Mobile_Renderer.asset` | 移动平台的 URP Renderer 配置。 |
| `Mobile_RPAsset.asset` | 移动平台的 URP 渲染管线资产。 |
| `PC_Renderer.asset` | PC 平台的 URP Renderer 配置。 |
| `PC_RPAsset.asset` | PC 平台的 URP 渲染管线资产。 |
| `UniversalRenderPipelineGlobalSettings.asset` | URP 全局 Shader、渲染和默认资源设置。 |

## TextMesh Pro `Assets/TextMesh Pro`

这些文件来自 Unity TextMesh Pro 默认资源导入，用于 UI 文本渲染。

| 文件 | 功能 |
|---|---|
| `Fonts/LiberationSans.ttf` | 默认 Liberation Sans 字体源文件。 |
| `Fonts/LiberationSans - OFL.txt` | Liberation Sans 字体许可证。 |
| `Resources/TMP Settings.asset` | TextMesh Pro 全局默认设置。 |
| `Resources/LineBreaking Leading Characters.txt` | 行首禁则字符表。 |
| `Resources/LineBreaking Following Characters.txt` | 行尾禁则字符表。 |
| `Resources/Fonts & Materials/LiberationSans SDF.asset` | 默认 SDF 字体图集和字形数据。 |
| `Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` | 默认后备字体资产。 |
| `Resources/Fonts & Materials/LiberationSans SDF - Drop Shadow.mat` | 带投影的默认文本材质。 |
| `Resources/Fonts & Materials/LiberationSans SDF - Outline.mat` | 带描边的默认文本材质。 |
| `Resources/Style Sheets/Default Style Sheet.asset` | TMP 默认富文本样式表。 |
| `Shaders/SDFFunctions.hlsl` | SDF 文本渲染共用函数。 |
| `Shaders/TMPro.cginc`<br>`TMPro_Mobile.cginc`<br>`TMPro_Properties.cginc`<br>`TMPro_Surface.cginc` | TMP 各 Shader 共用的 include 实现、移动优化、属性和表面着色代码。 |
| `Shaders/TMP_Bitmap.shader`<br>`TMP_Bitmap-Mobile.shader`<br>`TMP_Bitmap-Custom-Atlas.shader` | 普通、移动和自定义图集的位图字体 Shader。 |
| `Shaders/TMP_SDF.shader`<br>`TMP_SDF Overlay.shader`<br>`TMP_SDF SSD.shader` | 标准、覆盖层和 SSD 变体的 SDF 字体 Shader。 |
| `Shaders/TMP_SDF-Mobile.shader`<br>`TMP_SDF-Mobile Masking.shader`<br>`TMP_SDF-Mobile Overlay.shader`<br>`TMP_SDF-Mobile SSD.shader`<br>`TMP_SDF-Mobile-2-Pass.shader` | 面向移动平台的 SDF、遮罩、覆盖、SSD 和双通道变体。 |
| `Shaders/TMP_SDF-Surface.shader`<br>`TMP_SDF-Surface-Mobile.shader` | 标准与移动平台的 Surface Shader 变体。 |
| `Shaders/TMP_SDF-URP Lit.shadergraph`<br>`TMP_SDF-URP Unlit.shadergraph` | URP 光照/无光照 SDF Shader Graph。 |
| `Shaders/TMP_SDF-HDRP LIT.shadergraph`<br>`TMP_SDF-HDRP UNLIT.shadergraph` | HDRP 光照/无光照 SDF Shader Graph；当前项目主要使用 URP，但资源随 TMP 一并存在。 |
| `Shaders/TMP_Sprite.shader` | TMP 内联 Sprite 渲染 Shader。 |

## Unity 模板资源 `Assets/TutorialInfo`

| 文件 | 功能 |
|---|---|
| `Layout.wlt` | Unity 模板欢迎页的编辑器窗口布局。 |
| `Icons/URP.png` | 欢迎页使用的 URP 图标。 |
| `Scripts/Readme.cs` | 定义欢迎页标题、章节、链接和资源引用的数据结构。 |
| `Scripts/Editor/ReadmeEditor.cs` | 欢迎页自定义 Inspector，可定位说明资产或移除模板教程内容。 |

## 包与项目设置

### `Packages`

| 文件 | 功能 |
|---|---|
| `manifest.json` | 声明项目直接依赖；包括 URP、Input System、UGUI、Timeline、AI Navigation、Unity MCP 等。 |
| `packages-lock.json` | 锁定直接和间接 Unity 包的解析版本及依赖关系，保证环境可复现。 |

### `ProjectSettings`

| 文件 | 功能 |
|---|---|
| `ProjectVersion.txt` | 固定 Unity 编辑器版本为 `6000.4.11f1`。 |
| `ProjectSettings.asset` | 产品名、公司名、平台、分辨率、脚本后端等 Player Settings。 |
| `EditorBuildSettings.asset` | 构建时包含的场景列表及顺序。 |
| `EditorSettings.asset` | 序列化、资源管线、Prefab 和编辑器行为设置。 |
| `GraphicsSettings.asset` | Shader、渲染管线与全局图形设置。 |
| `QualitySettings.asset` | 各质量档位和平台默认质量配置。 |
| `URPProjectSettings.asset` | URP 项目级设置。 |
| `ShaderGraphSettings.asset` | Shader Graph 项目设置。 |
| `VFXManager.asset` | Visual Effect Graph 全局设置。 |
| `AudioManager.asset` | 音频系统全局设置。 |
| `DynamicsManager.asset` | 3D 物理、重力和碰撞相关设置。 |
| `Physics2DSettings.asset` | 2D 物理系统设置。 |
| `TimeManager.asset` | Fixed Timestep、时间缩放等时间设置。 |
| `InputManager.asset` | 旧版 Input Manager 的轴与按键配置。 |
| `ClusterInputManager.asset` | 集群显示输入配置。 |
| `TagManager.asset` | Tag、Layer 和 Sorting Layer 定义。 |
| `NavMeshAreas.asset` | NavMesh 区域名称和代价设置。 |
| `MemorySettings.asset` | Unity 内存分配器设置。 |
| `MultiplayerManager.asset` | 多人游戏相关的项目配置。 |
| `PackageManagerSettings.asset` | Package Manager Registry 和作用域设置。 |
| `PresetManager.asset` | 默认 Preset 关联规则。 |
| `SceneTemplateSettings.json` | 新建场景模板的编辑器配置。 |
| `UnityConnectSettings.asset` | Unity Services/云服务连接设置。 |
| `VersionControlSettings.asset` | Unity 版本控制模式和资源可见性设置。 |
| `XRSettings.asset` | XR/VR 兼容配置。 |

## 工具脚本 `Tools`

| 文件 | 功能 |
|---|---|
| `Blender/generate_six_spirits.py` | Blender Python 入口脚本，程序化生成六类酒瓶网格/材质并执行几何 QA 与统计。 |
| `Blender/__pycache__/generate_six_spirits.cpython-312.pyc` | Python 3.12 自动生成的字节码缓存，可删除并由解释器重建。 |

## 自动生成目录

| 目录 | 功能与处理建议 |
|---|---|
| `Library/` | Unity 导入后的资源数据库、包缓存和编译缓存；关闭 Unity 后可删除并重新导入。 |
| `Temp/` | Unity 当前会话的临时构建/编译文件；不要提交。 |
| `Logs/` | 编辑器及工具运行日志；用于排错，不作为项目源文件。 |
| `UserSettings/` | 当前用户的编辑器布局和偏好；通常不在团队间共享。 |

## 主要运行关系

```text
玩家输入
  └─ DrinkTestManager
      ├─ DragController / PourTiltFeedback / SelectionHighlight
      ├─ PourableIngredient → DrinkContainer → LiquidVisualController
      ├─ ShakerController
      └─ DrinkScoreSystem → TasteFeedbackSystem / DrinkScoreFeedbackFormatter

DemoRoundManager → TargetDrinkData → OrderDisplayController

GestureTemplateRecorder → GestureTemplateUtility → GestureTemplateLibrary
  └─ FlairGestureRecognizer → FlairGestureController → FlairableTool

DemoModeController / BuildReadinessValidator / DemoSetupValidator
  └─ 控制展示模式并检查场景配置
```
