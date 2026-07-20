# Tool Prefab Workflow

Recommended root structure:

```text
Tool_Root
└── VisualRoot
    ├── Tool_Model
    └── Liquid_Visual
```

Bottle:

```text
Bottle_LemonJuice_Test
└── VisualRoot
    └── Bottle_Model
```

Jigger / Shaker / Cup:

```text
Jigger_Test
└── VisualRoot
    ├── Jigger_Model
    └── Liquid_Visual
```

Root object owns:

- Collider
- Rigidbody
- InteractableObject
- DrinkContainer or PourableIngredient
- SelectionHighlight
- PourTiltFeedback
- FlairableTool

VisualRoot owns:

- Model display
- Flair code animation
- Pour tilt feedback visuals

When copying a tool, only change ingredient data, display color, `FlairableTool.actions`, `toolType`, position, and target container references.
