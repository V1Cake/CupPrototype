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

        // 当前工具在花式系统中的分类，用于限制模板动作，不等同于 DrinkContainer.ContainerType。
        public FlairToolType toolType = FlairToolType.Any;

        // 有精确 templateId 但本工具未配置对应动作时，是否允许退回通用 gestureType 动作。
        public bool allowGestureTypeFallback = true;

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

                if (!ToolTypeMatches(action))
                {
                    Debug.LogWarning($"[FlairableTool] Action {action.actionName} requires {action.requiredToolType}, but {name} is {toolType}.", this);
                }
            }
        }

        // 从动作簿里查找指定通用手势对应的花式动作。
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
                    actions[i].gestureType == gesture &&
                    ToolTypeMatches(actions[i]))
                {
                    action = actions[i];
                    return true;
                }
            }

            return false;
        }

        public bool TryGetAction(GestureMatchResult match, out FlairActionDefinition action)
        {
            return TryGetAction(match, out action, out _);
        }

        // 动作簿查找唯一入口：先精确 templateId，再按配置决定是否 fallback 到 gestureType。
        public bool TryGetAction(GestureMatchResult match, out FlairActionDefinition action, out string matchMode)
        {
            action = null;
            matchMode = "NoMatch";
            if (!enableFlair || !match.isMatched)
            {
                return false;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                FlairActionDefinition candidate = actions[i];
                if (candidate != null &&
                    !string.IsNullOrEmpty(candidate.requiredTemplateId) &&
                    candidate.requiredTemplateId == match.templateId &&
                    ToolTypeMatches(candidate))
                {
                    action = candidate;
                    matchMode = "TemplateId";
                    Debug.Log($"[FlairableTool] Matched action by templateId: {match.templateId} -> {action.actionName}", this);
                    return true;
                }
            }

            if (!allowGestureTypeFallback && !string.IsNullOrEmpty(match.templateId))
            {
                matchMode = "FallbackDisabled";
                Debug.Log($"[FlairableTool] Fallback disabled on {name} for template: {match.templateId}", this);
                return false;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                FlairActionDefinition candidate = actions[i];
                if (candidate != null &&
                    string.IsNullOrEmpty(candidate.requiredTemplateId) &&
                    candidate.gestureType == match.gestureType &&
                    ToolTypeMatches(candidate))
                {
                    action = candidate;
                    matchMode = "GestureTypeFallback";
                    Debug.Log($"[FlairableTool] Matched action by gestureType: {match.gestureType} -> {action.actionName}", this);
                    return true;
                }
            }

            matchMode = "NoAction";
            return false;
        }

        // 检查动作的工具限制是否允许当前工具触发。
        private bool ToolTypeMatches(FlairActionDefinition action)
        {
            return action == null ||
                action.requiredToolType == FlairToolType.Any ||
                action.requiredToolType == toolType;
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
                        visualRoot.localRotation = activeRoutineStartRotation * Quaternion.AngleAxis(action.spinDegrees * t, spinAxis);
                        break;
                    case FlairTestAnimationType.Flip:
                        visualRoot.localRotation = activeRoutineStartRotation * Quaternion.AngleAxis(action.spinDegrees * t, Vector3.right);
                        break;
                    case FlairTestAnimationType.Roll:
                        visualRoot.localRotation = activeRoutineStartRotation * Quaternion.AngleAxis(action.spinDegrees * t, Vector3.forward);
                        break;
                    case FlairTestAnimationType.Shake:
                        visualRoot.localPosition = activeRoutineStartPosition + Vector3.right * (Mathf.Sin(t * shakeCount * Mathf.PI * 2f) * action.shakeDistance);
                        break;
                    case FlairTestAnimationType.Swirl:
                        float angle = action.spinDegrees * t * Mathf.Deg2Rad;
                        visualRoot.localPosition = activeRoutineStartPosition + new Vector3(Mathf.Sin(angle), 0f, 1f - Mathf.Cos(angle)) * action.shakeDistance;
                        break;
                    case FlairTestAnimationType.PopUp:
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
