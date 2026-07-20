# Gesture Template Naming

Keep `templateId` stable. `FlairActionDefinition.requiredTemplateId` depends on it, so changing an ID means updating every matching `FlairableTool.actions` entry.

Recommended names:

- `BottleLoop_01`: Bottle-only loop gesture.
- `CupSwirl_01`: Cup swirl gesture.
- `ShakerRoll_01`: Shaker roll gesture.
- `Circle_Generic_01`: Generic Circle gesture.

Use tool-specific prefixes for exact-match actions and `Generic` only for fallback actions that are safe on more than one tool.
