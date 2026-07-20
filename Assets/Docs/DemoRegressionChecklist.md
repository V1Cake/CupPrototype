# Demo Regression Checklist

- Empty container transfer should fail safely.
- Full container pour should stop and show `Container is full`.
- Jigger should accept a single ingredient.
- Jigger should reject mixed or different ingredients.
- Shaker `ShakeLevel` and `MixState` should reset after clear.
- Flair playback should not accidentally trigger pouring.
- Switching flair while dragging should not leave input stuck.
- `R` reset should clear containers, selections, score feedback, and round state.
- Empty template library should make gesture recognition fail safely.
- Missing `requiredTemplateId` should fallback or reject based on the tool setting.
- `allowGestureTypeFallback=false` should reject gestureType fallback.
