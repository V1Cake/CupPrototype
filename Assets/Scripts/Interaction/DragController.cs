using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using UnityEngine;

namespace CupPrototype.Interaction
{
    // ===== 台面拖拽控制器 =====
    // 挂在 GameManager 上，负责把带 InteractableObject 的物体拖动到固定 Y 平面上。
    public class DragController : MonoBehaviour
    {
        // ===== Inspector 绑定参数 =====
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float tabletopY = 0f;
        [SerializeField] private LayerMask draggableLayers = ~0;
        [SerializeField] private float raycastDistance = 1000f;

        // ===== 当前拖拽状态 =====
        private InteractableObject currentObject;
        private InteractionCoordinator coordinator;
        // 只读暴露真实拖拽对象，供独立视觉反馈使用；不改变拖动逻辑。
        public InteractableObject CurrentDraggedObject => currentObject;
        private Vector3 dragOffset;
        private Plane dragPlane;
        private float draggedObjectFixedY;

        // ===== 生命周期：初始化相机和默认平面 =====
        private void Awake()
        {
            coordinator = GetComponent<InteractionCoordinator>();
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            // 初始化默认拖拽平面，真正拖拽时会按物体自身高度重建。
            dragPlane = new Plane(Vector3.up, new Vector3(0f, tabletopY, 0f));
        }

        // ===== 每帧输入入口 =====
        private void Update()
        {
            if (coordinator && coordinator.OwnsHeldInput) { StopDrag(); return; }
            // 花式或模板录制接管鼠标输入时，防止画轨迹同时拖动物体。
            if (FlairGestureController.IsFlairInputActive ||
                FlairGestureController.IsFlairPlaying ||
                GestureTemplateRecorder.IsTemplateRecording)
            {
                StopDrag();
                return;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryStartDrag();
            }

            if (Input.GetMouseButton(0) && currentObject != null)
            {
                DragCurrentObject();
            }

            if (Input.GetMouseButtonUp(0))
            {
                StopDrag();
            }
        }

        // ===== 开始拖拽 =====
        private void TryStartDrag()
        {
            if (FlairGestureController.IsFlairInputActive ||
                FlairGestureController.IsFlairPlaying ||
                GestureTemplateRecorder.IsTemplateRecording)
            {
                StopDrag();
                return;
            }

            // 只在按下瞬间用 Raycast 判断点中了哪个可拖拽物体。
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, draggableLayers))
            {
                return;
            }

            // 子物体被点中时，向父级查找 InteractableObject。
            InteractableObject interactable = hit.collider.GetComponentInParent<InteractableObject>();
            if (interactable == null)
            {
                return;
            }

            // 材料瓶点击用于选择材料，不进入拖拽。
            if (hit.collider.GetComponentInParent<PourableIngredient>() != null)
            {
                return;
            }

            DrinkContainer drinkContainer = hit.collider.GetComponentInParent<DrinkContainer>();

            // Shift 保留给 Jigger / Shaker 的容器移液，避免脚本执行顺序导致同一按键同时启动拖动。
            if (drinkContainer != null &&
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                return;
            }

            // 已选中材料时，杯子点击用于持续倒入，不进入拖拽。
            if (DrinkTestManager.HasSelectedIngredient && drinkContainer != null)
            {
                return;
            }

            currentObject = interactable;
            draggedObjectFixedY = currentObject.transform.position.y;

            // 容器 Root 高度不同，统一使用命中 Collider 中心投影鼠标；实际 Root 的 Y 仍保持不变。
            float dragPlaneY = drinkContainer != null
                ? hit.collider.bounds.center.y
                : draggedObjectFixedY;

            dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));

            Debug.Log($"[DragController] Start drag {currentObject.name}, fixedY={draggedObjectFixedY}", currentObject);

            if (TryGetMousePointOnDragPlane(out Vector3 mousePoint))
            {
                dragOffset = currentObject.transform.position - mousePoint;
                dragOffset.y = 0f;
            }
            else
            {
                dragOffset = Vector3.zero;
            }
        }

        // ===== 拖拽中：只更新 X/Z，锁定 Y =====
        private void DragCurrentObject()
        {
            if (FlairGestureController.IsFlairInputActive ||
                FlairGestureController.IsFlairPlaying ||
                GestureTemplateRecorder.IsTemplateRecording)
            {
                StopDrag();
                return;
            }

            // 拖拽过程中只用固定高度平面计算鼠标位置，不再依赖任何 collider hit.point。
            if (!TryGetMousePointOnDragPlane(out Vector3 mousePoint))
            {
                return;
            }

            Vector3 targetPosition = mousePoint + dragOffset;
            targetPosition.y = draggedObjectFixedY;
            currentObject.transform.position = targetPosition;
        }

        // ===== 鼠标投影到拖拽平面 =====
        private bool TryGetMousePointOnDragPlane(out Vector3 point)
        {
            // 把鼠标射线投到当前拖拽物体的固定 Y 平面上。
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

            if (dragPlane.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        // ===== 结束拖拽 =====
        private void StopDrag()
        {
            if (currentObject != null)
            {
                Debug.Log($"[DragController] End drag {currentObject.name}", currentObject);
            }

            currentObject = null;
            dragOffset = Vector3.zero;
        }

        // ===== Inspector 参数变化时刷新默认平面 =====
        private void OnValidate()
        {
            // Inspector 修改 tabletopY 时刷新默认平面；实际拖拽仍以物体初始 Y 为准。
            dragPlane = new Plane(Vector3.up, new Vector3(0f, tabletopY, 0f));
        }
    }
}
