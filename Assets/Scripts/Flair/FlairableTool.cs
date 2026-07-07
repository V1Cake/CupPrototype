using CupPrototype.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 挂在支持花式的工具上，保存该工具自己的动作簿。
    public class FlairableTool : MonoBehaviour
    {
        // 只旋转演出根节点，避免影响根对象 Collider、Rigidbody 和交互逻辑。
        public Transform visualRoot;

        // 是否允许该工具触发花式。
        public bool enableFlair = true;

        // 该工具可用花式列表；每一条都是“手势到花式动作”的配置项。
        public List<FlairActionDefinition> actions = new List<FlairActionDefinition>();

        private Coroutine activeRoutine;
        private Quaternion activeRoutineStartRotation;
        private PourTiltFeedback activeTiltFeedback;
        private bool previousEnableTilt;
        private bool previousTiltComponentEnabled;

        // 从动作簿里查找指定手势对应的花式动作。
        public bool TryGetAction(FlairGestureType gesture, out FlairActionDefinition action)
        {
            action = null;
            if (!enableFlair)
            {
                return false;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i] != null && actions[i].gestureType == gesture)
                {
                    action = actions[i];
                    return true;
                }
            }

            return false;
        }

        // 第一版只播放测试旋转动画，不改数据、不改根对象、不走正式 Animator。
        public void PlayFlair(FlairActionDefinition action)
        {
            if (action == null)
            {
                return;
            }

            if (visualRoot == null)
            {
                Debug.LogWarning("[FlairableTool] visualRoot is missing. Flair skipped.", this);
                return;
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                visualRoot.localRotation = activeRoutineStartRotation;
                RestoreTiltFeedback();
                FlairGestureController.SetFlairPlaying(false);
            }

            activeRoutine = StartCoroutine(PlaySpinRoutine(action));
        }

        private void OnDisable()
        {
            if (activeRoutine != null && visualRoot != null)
            {
                StopCoroutine(activeRoutine);
                visualRoot.localRotation = activeRoutineStartRotation;
            }

            RestoreTiltFeedback();
            FlairGestureController.SetFlairPlaying(false);
            activeRoutine = null;
        }

        private IEnumerator PlaySpinRoutine(FlairActionDefinition action)
        {
            FlairGestureController.SetFlairPlaying(true);
            activeRoutineStartRotation = visualRoot.localRotation;
            activeTiltFeedback = GetComponent<PourTiltFeedback>();
            if (activeTiltFeedback != null)
            {
                previousEnableTilt = activeTiltFeedback.enableTilt;
                previousTiltComponentEnabled = activeTiltFeedback.enabled;
                activeTiltFeedback.enableTilt = false;
                activeTiltFeedback.enabled = false;
            }

            float duration = Mathf.Max(0.01f, action.duration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 spinAxis = action.spinAxis.sqrMagnitude > Mathf.Epsilon
                    ? action.spinAxis.normalized
                    : Vector3.forward;
                visualRoot.localRotation = activeRoutineStartRotation * Quaternion.AngleAxis(action.spinDegrees * t, spinAxis);
                yield return null;
            }

            visualRoot.localRotation = activeRoutineStartRotation;
            RestoreTiltFeedback();
            FlairGestureController.SetFlairPlaying(false);
            activeRoutine = null;
        }

        private void RestoreTiltFeedback()
        {
            if (activeTiltFeedback != null)
            {
                activeTiltFeedback.enabled = previousTiltComponentEnabled;
                activeTiltFeedback.enableTilt = previousEnableTilt;
                activeTiltFeedback = null;
            }
        }
    }
}
