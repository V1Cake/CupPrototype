# P5-3 Manual Test Plan

## T1 Idle

进入场景，不选任何对象。

预期：

无 Outline。

---

## T2 Selection Switching

依次点击：

Bottle
→ Jigger
→ Shaker
→ Cup

预期：

只有当前对象有 Outline。

旧对象 Outline 会关闭。

---

## T3 Held

选择一个可拖动物体。

进入 Drag / Held。

移动。

释放。

预期：

Held 期间 Outline 保持。

释放后状态恢复正确。

无闪烁。

无残留。

---

## T4 Gameplay Regression

执行：

Bottle → Jigger

Jigger → Shaker

Shaker → Cup

预期：

所有原转移规则保持正常。

Outline 不影响液体、容量和材质。

---

## T5 Reset

选中物体后执行 Reset。

预期：

无 Outline 残留。

---

## T6 Order Change

切换测试订单。

预期：

Selection 状态正常。

无残留。

---

## T7 Gesture / Flair

执行一次已有 Gesture / Flair。

预期：

原功能不受影响。

---

## T8 Console

检查 Unity Console。

预期：

无新增 Error。

无 NullReference。

## PASS

全部通过后：

P5-3 = PASS / Frozen Prototype
