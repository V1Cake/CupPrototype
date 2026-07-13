using UnityEngine;

namespace CupPrototype.Flair
{
    // 测试阶段用的代码动画类型，未来可被 Animator Trigger 替代。
    public enum FlairTestAnimationType
    {
        // 绕本地 Y 轴旋转一圈，适合 Bottle。
        Spin,
        // 绕本地 X 轴翻转一圈，适合 Jigger。
        Flip,
        // 绕本地 Z 轴滚动一圈，适合 Shaker。
        Roll,
        // 沿本地 X 轴左右快速晃动，适合 Shaker 或 Cup。
        Shake,
        // 在 X/Z 平面轻微绕圈晃动，适合 Cup。
        Swirl,
        // 上抬/轻抛预演测试动画，只移动 visualRoot，不使用 Rigidbody。
        PopUp
    }

    // FlairActionDefinition 是动作簿中的一条动作记录：把一个手势映射到某个花式动作配置。
    [System.Serializable]
    public class FlairActionDefinition
    {
        // 动作名称，例如 BottleSpinBasic。
        public string actionName;

        // 触发该动作的花式手势。
        public FlairGestureType gestureType;

        // 绑定到某个开发者手绘模板；为空时按 gestureType 作为通用动作匹配。
        public string requiredTemplateId;

        // 预留给未来 Animator Controller 使用，本阶段可以为空。
        public string animationTrigger;

        // 临时测试动画类型；未来可以用 animationTrigger 替换代码动画。
        public FlairTestAnimationType testAnimationType = FlairTestAnimationType.Spin;

        // 测试代码动画持续时间。
        public float duration = 0.5f;

        // 测试旋转类动画角度。
        public float spinDegrees = 360f;

        // Spin 的测试旋转轴；模型轴向不明显时可在动作簿里改成 X 或 Z。
        public Vector3 spinAxis = Vector3.up;

        // 测试晃动位移幅度。
        public float shakeDistance = 0.08f;

        // 测试晃动次数。
        public int shakeCount = 4;

        // PopUp 测试动画的上抬高度，不代表真实物理抛接。
        public float popUpHeight = 0.25f;

        // 预留字段，本阶段不实现 QTE。
        public bool requiresQTE = false;

        // 预留字段，本阶段不实现失败逻辑。
        public bool canFail = false;
    }
}
