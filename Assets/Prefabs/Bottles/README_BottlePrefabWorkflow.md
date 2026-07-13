# Bottle Prefab Workflow

## 推荐结构

```text
Bottle_xxx
└── VisualRoot
    └── Bottle_Model
```

## 根对象组件

- Collider
- Rigidbody
- InteractableObject
- PourableIngredient
- SelectionHighlight
- PourTiltFeedback
- FlairableTool

## 新建瓶子通常只需要改

- 对象名
- PourableIngredient.ingredient
- Bottle_Model 材质
- 位置
- FlairableTool.actions 动作簿，如有特殊花式

PourTiltFeedback 和 FlairableTool 会自动查找 VisualRoot。

## 测试动作簿配置示例

Bottle_LemonJuice_Test:

- FlairableTool.actions[0]
- actionName = BottleSpinBasic
- gestureType = Circle
- testAnimationType = Spin
- duration = 0.5
- spinDegrees = 360

Jigger_Test:

- 添加 FlairableTool，如果还没有
- visualRoot 自动绑定或手动绑定 VisualRoot
- actions[0]
- actionName = JiggerFlipBasic
- gestureType = Circle
- testAnimationType = Flip
- duration = 0.5
- spinDegrees = 360

Shaker_Test:

- 添加 FlairableTool，如果还没有
- visualRoot 自动绑定或手动绑定 VisualRoot
- actions[0]
- actionName = ShakerRollBasic
- gestureType = Circle
- testAnimationType = Roll
- duration = 0.5
- spinDegrees = 360

Cup_Test:

```text
Cup_Test
└── VisualRoot
    ├── Cup_Model
    └── Liquid_Visual
```

- 如果 Liquid_Visual 当前不适合跟随杯子旋转，可以先保持原结构；长期建议 Cup_Model 和 Liquid_Visual 都在 VisualRoot 下
- 添加 FlairableTool，如果还没有
- visualRoot 自动绑定或手动绑定 VisualRoot
- actions[0]
- actionName = CupSwirlBasic
- gestureType = Circle
- testAnimationType = Swirl
- duration = 0.5
- spinDegrees = 360
- requiresQTE = false
- canFail = false
