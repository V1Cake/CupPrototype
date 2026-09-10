using System.Collections;
using UnityEngine;

namespace CupPrototype.UI
{
    // P5-2A 临时原型：仅在物理屏幕与当前 Gameplay 视角之间往返。
    public sealed class DisplayFocusPrototype : MonoBehaviour
    {
        [SerializeField] private Transform focusAnchor;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private float transitionDuration = 0.25f;

        private Vector3 normalPosition;
        private Quaternion normalRotation;
        private Coroutine transition;

        public bool IsFocused { get; private set; }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetFocused(!IsFocused);
            }
            else if (IsFocused && Input.GetKeyDown(KeyCode.Escape))
            {
                SetFocused(false);
            }
        }

        public void SetFocused(bool focused)
        {
            if (GetComponent<CupPrototype.Interaction.MeasurementCameraLock>()?.IsLocked == true) return;
            if (focused && focusAnchor == null)
            {
                Debug.LogWarning("[DisplayFocusPrototype] Focus Anchor is missing.", this);
                return;
            }

            if (focused)
            {
                normalPosition = transform.position;
                normalRotation = transform.rotation;
            }

            IsFocused = focused;
            if (transition != null)
            {
                StopCoroutine(transition);
            }

            Vector3 targetPosition = focused ? focusAnchor.position : normalPosition;
            Quaternion targetRotation = focused ? focusAnchor.rotation : normalRotation;
            transition = StartCoroutine(MoveTo(targetPosition, targetRotation));
        }

        private IEnumerator MoveTo(Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float duration = Mathf.Max(0.01f, transitionDuration);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                while (GetComponent<CupPrototype.Interaction.MeasurementCameraLock>()?.IsLocked == true) yield return null;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, t),
                    Quaternion.Slerp(startRotation, targetRotation, t));
                yield return null;
            }

            transform.SetPositionAndRotation(targetPosition, targetRotation);
            transition = null;
        }
    }
}
