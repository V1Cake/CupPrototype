using CupPrototype.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace CupPrototype.UI
{
    public sealed class ShakerContextUI : MonoBehaviour
    {
        [SerializeField] private InteractionCoordinator coordinator;
        [SerializeField] private Button closeButton;
        public bool IsCloseVisible => closeButton && closeButton.gameObject.activeInHierarchy;
        private void Awake() { closeButton.onClick.AddListener(CloseShaker); Refresh(); }
        private void OnDestroy() { if (closeButton) closeButton.onClick.RemoveListener(CloseShaker); }
        private void CloseShaker() { if (coordinator) coordinator.TryCloseShaker(); Refresh(); }
        private void LateUpdate() => Refresh();
        public void Refresh() { if (closeButton) closeButton.gameObject.SetActive(coordinator && coordinator.CanCloseShaker); }
    }
}
