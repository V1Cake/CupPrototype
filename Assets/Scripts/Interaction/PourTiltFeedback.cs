using UnityEngine;

namespace CupPrototype.Interaction
{
    // 只做倒出时的视觉倾斜反馈，可用于 Jigger、Shaker、Bottle 等工具。
    public class PourTiltFeedback : MonoBehaviour
    {
        // 必须绑定模型子物体，避免影响根对象、Collider、Rigidbody 和后续动画。
        public Transform visualRoot;
        public float tiltAngle = 18f;
        [Tooltip("仅控制模型旋转到倾斜姿态的速度，不控制液体流速。Bottle → Jigger 流速请调整 Pourable Ingredient 的 Pour Rate Per Second。")]
        public float tiltSpeed = 8f;
        public bool enableTilt = true;

        private Quaternion initialLocalRotation;
        private Quaternion targetLocalRotation;

        private void Awake()
        {
            if (visualRoot == null)
            {
                // 批量创建工具 Prefab 时自动找 VisualRoot，减少手动绑定。
                visualRoot = transform.Find("VisualRoot");
                if (visualRoot != null)
                {
                    Debug.Log($"[PourTiltFeedback] Auto-bound VisualRoot on {name}", this);
                }
            }

            if (visualRoot == null)
            {
                Debug.LogWarning("[PourTiltFeedback] visualRoot is missing. Tilt feedback skipped.", this);
                return;
            }

            initialLocalRotation = visualRoot.localRotation;
            targetLocalRotation = initialLocalRotation;
        }

        private void Update()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                targetLocalRotation,
                tiltSpeed * Time.deltaTime);
        }

        // 开始让模型子物体朝目标位置倾斜。
        public void StartTiltTowards(Vector3 targetWorldPosition)
        {
            if (!enableTilt || visualRoot == null)
            {
                return;
            }

            Vector3 direction = targetWorldPosition - visualRoot.position;
            Vector3 localDirection = visualRoot.parent != null
                ? visualRoot.parent.InverseTransformDirection(direction)
                : direction;
            localDirection.y = 0f;

            if (localDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                targetLocalRotation = initialLocalRotation;
                return;
            }

            Vector3 tiltAxis = Vector3.Cross(Vector3.up, localDirection.normalized);
            targetLocalRotation = Quaternion.AngleAxis(tiltAngle, tiltAxis) * initialLocalRotation;
        }

        // 停止倾斜，并让模型子物体平滑回到初始角度。
        public void StopTilt()
        {
            if (visualRoot != null)
            {
                targetLocalRotation = initialLocalRotation;
            }
        }
    }
}
