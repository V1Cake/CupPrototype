using CupPrototype.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace CupPrototype.UI
{
    public sealed class ShakerContextUI : MonoBehaviour
    {
        [SerializeField] private InteractionCoordinator coordinator;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;
        public bool IsCloseVisible => closeButton && closeButton.gameObject.activeInHierarchy;
        public bool IsOpenVisible => openButton && openButton.gameObject.activeInHierarchy;
        private void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(CloseShaker);
            if (openButton) openButton.onClick.AddListener(OpenShaker);
            Refresh();
        }
        private void OnDestroy()
        {
            if (closeButton) closeButton.onClick.RemoveListener(CloseShaker);
            if (openButton) openButton.onClick.RemoveListener(OpenShaker);
        }
        private void CloseShaker() { if (coordinator) coordinator.TryCloseShaker(); Refresh(); }
        private void OpenShaker() { if (coordinator) coordinator.TryOpenShaker(); Refresh(); }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (closeButton) closeButton.gameObject.SetActive(coordinator && coordinator.CanCloseShaker);
            if (openButton) openButton.gameObject.SetActive(coordinator && coordinator.CanOpenShaker);
        }
    }
}
