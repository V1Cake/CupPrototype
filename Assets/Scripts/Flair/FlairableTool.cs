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

        // 防止同一工具在花式动画未结束时重复播放。
        public bool IsPlayingFlair { get; private set; }

        private Coroutine activeRoutine;
        private Quaternion activeRoutineStartRotation;
        private Vector3 activeRoutineStartPosition;
        private PourTiltFeedback activeTiltFeedback;
        private bool previousEnableTilt;

        private void Awake()
        {
            if (visualRoot == null)
            {
                // visualRoot 用于播放花式演出，避免影响根对象 Collider / Rigidbody。
                visualRoot = transform.Find("VisualRoot");
                if (visualRoot != null)
                {
                    Debug.Log($"[FlairableTool] Auto-bound VisualRoot on {name}", this);
                }
                else
                {
                    Debug.LogWarning($"[FlairableTool] visualRoot is missing on {name}. Flair skipped.", this);
                }
            }
        }

        private void Start()
        {
            ValidateActionBook();
        }

        // 检查动作簿配置错误，只提示，不自动修改动作簿。
        public void ValidateActionBook()
        {
            if (visualRoot == null)
            {
                Debug.LogWarning($"[FlairableTool] visualRoot is missing on {name}.", this);
            }

            if (actions == null || actions.Count == 0)
            {
                Debug.LogWarning($"[FlairableTool] Action book is empty on {name}", this);
                return;
            }

            HashSet<FlairGestureType> usedGestures = new HashSet<FlairGestureType>();
            for (int i = 0; i < actions.Count; i++)
            {
                FlairActionDefinition action = actions[i];
                if (action == null)
                {
                    continue;
                }

                if (action.gestureType == FlairGestureType.None)
                {
                    Debug.LogWarning($"[FlairableTool] Action book entry {i} has gestureType None on {name}", this);
                }

                if (string.IsNullOrEmpty(action.actionName))
                {
                    Debug.LogWarning($"[FlairableTool] Action book entry {i} has empty actionName on {name}", this);
                }

                if (action.gestureType != FlairGestureType.None && !usedGestures.Add(action.gestureType))
                {
                    Debug.LogWarning($"[FlairableTool] Duplicate gesture in action book: {action.gestureType}", this);
                }
            }
        }

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
                if (actions[i] != null &&
                    string.IsNullOrEmpty(actions[i].requiredTemplateId) &&
                    actions[i].gestureType == gesture)
                {
                    action = actions[i];
                    return true;
                }
            }

            return false;
        }

        // 动作簿查找优先级：templateId 精确匹配优先，找不到再用 gestureType 通用匹配。
        public bool TryGetAction(GestureMatchResult match, out FlairActionDefinition action)
        {
            action = null;
            if (!enableFlair || !match.isMatched)
            {
                return false;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                FlairActionDefinition candidate = actions[i];
                if (candidate != null &&
                    !string.IsNullOrEmpty(candidate.requiredTemplateId) &&
                    candidate.requiredTemplateId == match.templateId)
                {
                    action = candidate;
                    Debug.Log($"[FlairableTool] Matched action by templateId: {match.templateId} -> {action.actionName}", this);
                    return true;
                }
            }

            for (int i = 0; i < actions.Count; i++)
            {
                FlairActionDefinition candidate = actions[i];
                if (candidate != null &&
                    string.IsNullOrEmpty(candidate.requiredTemplateId) &&
                    candidate.gestureType == match.gestureType)
                {
                    action = candidate;
                    Debug.Log($"[FlairableTool] Matched action by gestureType: {match.gestureType} -> {action.actionName}", this);
                    return true;
                }
            }

            return false;
        }

        // 根据动作簿记录播放临时代码动画，不改数据、不改根对象、不走正式 Animator。
        public void PlayFlair(FlairActionDefinition action)
        {
            if (IsPlayingFlair)
            {
                Debug.Log($"[FlairableTool] Flair already playing on {name}", this);
                return;
            }

            if (action == null)
            {
                IsPlayingFlair = false;
                return;
            }

            if (visualRoot == null)
            {
                IsPlayingFlair = false;
                Debug.LogWarning("[FlairableTool] visualRoot is missing. Flair skipped.", this);
                return;
            }

            IsPlayingFlair = true;
            Debug.Log($"[FlairableTool] Playing {action.actionName}: {action.testAnimationType}", this);
            activeRoutine = StartCoroutine(PlayTestAnimationRoutine(action));
        }

        private void OnDisable()
        {
            if (activeRoutine != null && visualRoot != null)
            {
                StopCoroutine(activeRoutine);
                visualRoot.localRotation = activeRoutineStartRotation;
                visualRoot.localPosition = activeRoutineStartPosition;
            }

            RestoreTiltFeedback();
            FlairGestureController.SetFlairPlaying(false);
            IsPlayingFlair = false;
            activeRoutine = null;
        }

        private IEnumerator PlayTestAnimationRoutine(FlairActionDefinition action)
        {
            if (action == null || visualRoot == null)
            {
                FinishFlairPlayback();
                yield break;
            }

            FlairGestureController.SetFlairPlaying(true);
            activeRoutineStartRotation = visualRoot.localRotation;
            activeRoutineStartPosition = visualRoot.localPosition;
            activeTiltFeedback = GetComponent<PourTiltFeedback>();
            if (activeTiltFeedback != null)
            {
                previousEnableTilt = activeTiltFeedback.enableTilt;
                activeTiltFeedback.enableTilt = false;
            }

            float duration = Mathf.Max(0.01f, action.duration);
            float elapsed = 0f;
            int shakeCount = Mathf.Max(1, action.shakeCount);
            Vector3 spinAxis = action.spinAxis.sqrMagnitude > Mathf.Epsilon
                ? action.spinAxis.normalized
                : Vector3.up;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                switch (action.testAnimationType)
                {
                    case FlairTestAnimationType.Spin:
                        // Spin：动作簿绑定的 Bottle 测试旋转；模型轴向不明显时可调 spinAxis。
                        visualRoot.localRotation = activeRoutineStartRotation * Quaternion.AngleAxis(action.spinDegrees * t, spinAxis);
                        break;
                    case FlairTestAnimationType.Flip:
                        // Flip：动作簿绑定的 Jigger 测试翻转，未来可改用 animationTrigger。
                        visualRoot.localRotation = activeRoutineStartRotation * Quaternion.AngleAxis(action.spinDegrees * t, Vector3.right);
                        break;
                    case FlairTestAnimationType.Roll:
                        // Roll：动作簿绑定的 Shaker 测试滚动，未来可改用 animationTrigger。
                        visualRoot.localRotation = activeRoutineStartRotation * Quaternion.AngleAxis(action.spinDegrees * t, Vector3.forward);
                        break;
                    case FlairTestAnimationType.Shake:
                        // Shake：动作簿绑定的快速晃动，只移动 visualRoot。
                        visualRoot.localPosition = activeRoutineStartPosition + Vector3.right * (Mathf.Sin(t * shakeCount * Mathf.PI * 2f) * action.shakeDistance);
                        break;
                    case FlairTestAnimationType.Swirl:
                        // Swirl：Cup 也可以通过动作簿绑定自己的绕圈晃动。
                        float angle = action.spinDegrees * t * Mathf.Deg2Rad;
                        visualRoot.localPosition = activeRoutineStartPosition + new Vector3(Mathf.Sin(angle), 0f, 1f - Mathf.Cos(angle)) * action.shakeDistance;
                        break;
                    case FlairTestAnimationType.PopUp:
                        // PopUp：FlickUp 动作簿测试动画，只上抬 visualRoot，不做真实物理抛接。
                        float popT = t <= 0.5f ? t * 2f : (1f - t) * 2f;
                        visualRoot.localPosition = activeRoutineStartPosition + Vector3.up * (action.popUpHeight * popT);
                        break;
                    default:
                        visualRoot.localRotation = activeRoutineStartRotation * Quaternion.AngleAxis(action.spinDegrees * t, Vector3.up);
                        break;
                }

                yield return null;
            }

            FinishFlairPlayback();
        }

        private void RestoreTiltFeedback()
        {
            if (activeTiltFeedback != null)
            {
                activeTiltFeedback.enableTilt = previousEnableTilt;
                activeTiltFeedback = null;
            }
        }

        private void FinishFlairPlayback()
        {
            if (visualRoot != null)
            {
                visualRoot.localRotation = activeRoutineStartRotation;
                visualRoot.localPosition = activeRoutineStartPosition;
            }

            RestoreTiltFeedback();
            FlairGestureController.SetFlairPlaying(false);
            IsPlayingFlair = false;
            activeRoutine = null;
        }
    }
}
