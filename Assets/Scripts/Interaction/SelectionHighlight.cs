using System.Collections.Generic;
using CupPrototype.DrinkSystem;
using UnityEngine;

namespace CupPrototype.Interaction
{
    // 只保存视觉状态。未来手持/教程适配器也可调用 SetHighlighted，不负责操作规则。
    public class SelectionHighlight : MonoBehaviour
    {
        public enum VisualState { Idle, Selected, Held }
        public static readonly HashSet<SelectionHighlight> Active = new HashSet<SelectionHighlight>();
        public VisualState State { get; private set; }
        public Renderer[] VisualRenderers { get; private set; }
        [SerializeField, HideInInspector] private GameObject outlineObject;
        private bool selected;
        private DrinkContainer container;
        private InteractableObject interactable;
        private DrinkTestManager manager;
        private DragController drag;

        private void Awake()
        {
            // 停用旧场景中显式配置的发光替身，不触碰真实模型材质。
            if(outlineObject)outlineObject.SetActive(false);
            VisualRenderers=GetComponentsInChildren<Renderer>(true);
            container=GetComponent<DrinkContainer>();
            interactable=GetComponent<InteractableObject>();
            manager=FindAnyObjectByType<DrinkTestManager>();
            drag=FindAnyObjectByType<DragController>();
        }
        private void OnEnable(){Active.Add(this);}
        private void OnDisable(){Active.Remove(this);selected=false;State=VisualState.Idle;}
        public void SetHighlighted(bool highlighted)
        {
            selected=highlighted;
            RefreshState();
        }
        private void LateUpdate(){RefreshState();}
        private void RefreshState()
        {
            bool held=drag&&drag.isActiveAndEnabled&&interactable&&drag.CurrentDraggedObject==interactable;
            bool source=manager&&container&&manager.SelectedSourceContainer==container;
            State=held?VisualState.Held:selected||source?VisualState.Selected:VisualState.Idle;
        }
    }
}

