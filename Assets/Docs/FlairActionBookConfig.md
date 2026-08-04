# Flair 动作簿配置

## 动作类型

- 通用动作：`requiredTemplateId` 留空，通过 `gestureType` 匹配。
- 精确模板动作：`requiredTemplateId` 填写 `GestureTemplateAsset` 内部的 `templateId`。

同一 `gestureType` 可以同时配置通用动作和多个不同的精确模板动作。只有 `gestureType`、规范化后的 `requiredTemplateId`、`requiredToolType` 三项全部相同，才属于重复条件。

## 匹配优先级

1. `templateId` 精确匹配。
2. 检查 `allowGestureTypeFallback`。
3. 允许 fallback 时使用 `gestureType` 通用匹配。

`templateId` 区分大小写，内容和空格必须与 `GestureTemplateAsset.templateId` 完全一致。配置验证会忽略首尾空格，但运行时匹配不会替你修正错误 ID。

## 推荐工具配置

| 工具 | `toolType` | `allowGestureTypeFallback` |
| --- | --- | --- |
| Bottle | `Bottle` | `true` |
| Jigger | `Jigger` | `false` |
| Shaker | `Shaker` | `false` |
| Cup | `Cup` | `false` |
