using UnityEngine;

namespace CupPrototype.Interaction
{
    // ===== 可交互物体标记 =====
    // 只作为标记组件使用，DragController 会通过它判断物体是否允许拖拽。
    /// <summary>
    /// Marker component for objects that can be dragged on the tabletop.
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        // ===== 空组件 =====
        // 当前不保存数据，后续如果需要拖拽限制或类型信息，可以从这里扩展。
    }
}
