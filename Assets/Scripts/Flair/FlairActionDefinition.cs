using UnityEngine;

namespace CupPrototype.Flair
{
    // 测试阶段用的代码动画类型，未来可由 Animator Trigger 替代。
    public enum FlairTestAnimationType
    {
        Spin,
        Flip,
        Roll,
        Shake,
        Swirl,
        PopUp
    }

    // 动作簿中的一条动作记录：把手势映射到一个花式动作配置。
    [System.Serializable]
    public class FlairActionDefinition
    {
        // 动作名称，例如 BottleSpinBasic。
        public string actionName;

        // 触发该动作的通用手势类型。
        public FlairGestureType gestureType;

        // 精确绑定的开发者模板；为空时按 gestureType 通用匹配。
        public string requiredTemplateId;

        // 限制这条动作只能被指定工具类型触发；Any 表示不限制。
        public FlairToolType requiredToolType = FlairToolType.Any;

        // 预留给未来 Animator Controller 使用。
        public string animationTrigger;

        // 当前 Demo 用代码动画预览花式动作。
        public FlairTestAnimationType testAnimationType = FlairTestAnimationType.Spin;

        // 测试代码动画持续时间。
        public float duration = 0.5f;

        // 测试旋转类动画角度。
        public float spinDegrees = 360f;

        // Spin 的测试旋转轴，模型轴向不明显时可在动作簿里调整。
        public Vector3 spinAxis = Vector3.up;

        // 测试晃动位移幅度。
        public float shakeDistance = 0.08f;

        // 测试晃动次数。
        public int shakeCount = 4;

        // PopUp 测试动画的上抛高度，不代表真实物理抛接。
        public float popUpHeight = 0.25f;

        // 预留字段，本阶段不实现 QTE。
        public bool requiresQTE = false;

        // 预留字段，本阶段不实现失败逻辑。
        public bool canFail = false;
    }
}
