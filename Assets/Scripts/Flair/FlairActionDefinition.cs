using UnityEngine;

namespace CupPrototype.Flair
{
    // 动作电话簿的一条记录：把一个手势映射到某个花式动作配置。
    [System.Serializable]
    public class FlairActionDefinition
    {
        // 动作名称，例如 BottleSpinBasic。
        public string actionName;

        // 触发该动作的花式手势。
        public FlairGestureType gestureType;

        // 预留给未来 Animator Controller 使用，本阶段可以为空。
        public string animationTrigger;

        // 第一版测试旋转动画持续时间。
        public float duration = 0.5f;

        // 第一版测试旋转动画角度。
        public float spinDegrees = 360f;

        // 测试旋转轴；酒瓶绕 Y 轴不明显时，可改成 X 或 Z 轴。
        public Vector3 spinAxis = Vector3.forward;

        // 预留字段，本阶段不实现 QTE。
        public bool requiresQTE = false;

        // 预留字段，本阶段不实现失败逻辑。
        public bool canFail = false;
    }
}
