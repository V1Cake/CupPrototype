using CupPrototype.Interaction;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CupPrototype.UI
{
    public sealed class ShakerContextUI : MonoBehaviour
    {
        [SerializeField] private InteractionCoordinator coordinator;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;
        [SerializeField] private TMP_Text tasteFeedback;
        [SerializeField] private GameObject tasteFeedbackRoot;
        [SerializeField] private TMP_Text shakeIceLabel;
        [SerializeField] private TMP_Text serveIceLabel;
        private CurrentDrinkRecord record;
        public bool IsCloseVisible => closeButton && closeButton.gameObject.activeInHierarchy;
        public bool IsOpenVisible => openButton && openButton.gameObject.activeInHierarchy;
        private void Awake()
        {
            record = GetComponent<CurrentDrinkRecord>();
            if (record) record.Changed += Refresh;
            if (closeButton) closeButton.onClick.AddListener(CloseShaker);
            if (openButton) openButton.onClick.AddListener(OpenShaker);
            Refresh();
        }
        private void OnDestroy()
        {
            if (record) record.Changed -= Refresh;
            if (closeButton) closeButton.onClick.RemoveListener(CloseShaker);
            if (openButton) openButton.onClick.RemoveListener(OpenShaker);
        }
        private void CloseShaker() { if (coordinator) coordinator.TryCloseShaker(); Refresh(); }
        private void OpenShaker() { if (coordinator) coordinator.TryOpenShaker(); Refresh(); }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (shakeIceLabel) shakeIceLabel.text = record && record.ProcessIce ? "SHAKE ICE\nADDED" : "SHAKE ICE";
            if (serveIceLabel) serveIceLabel.text = record && record.ServeIce ? "SERVE ICE\nADDED" : "SERVE ICE";
            if (tasteFeedback) tasteFeedback.text = record ? record.TasteFeedback : string.Empty;
            if (tasteFeedbackRoot) tasteFeedbackRoot.SetActive(record && !string.IsNullOrEmpty(record.TasteFeedback));
            if (closeButton) closeButton.gameObject.SetActive(coordinator && coordinator.CanCloseShaker);
            if (openButton) openButton.gameObject.SetActive(coordinator && coordinator.CanOpenShaker);
        }
    }
}
