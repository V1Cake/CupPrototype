using CupPrototype.DrinkSystem;
using UnityEngine;

namespace CupPrototype.Interaction
{
    public class DragController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float tabletopY = 0f;
        [SerializeField] private LayerMask draggableLayers = ~0;
        [SerializeField] private float raycastDistance = 1000f;

        private InteractableObject currentObject;
        private Vector3 dragOffset;
        private Plane dragPlane;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            dragPlane = new Plane(Vector3.up, new Vector3(0f, tabletopY, 0f));
        }

        private void Update()
        {
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

        private void TryStartDrag()
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, draggableLayers))
            {
                return;
            }

            InteractableObject interactable = hit.collider.GetComponentInParent<InteractableObject>();
            if (interactable == null)
            {
                return;
            }

            if (hit.collider.GetComponentInParent<PourableIngredient>() != null)
            {
                return;
            }

            if (DrinkTestManager.HasSelectedIngredient &&
                hit.collider.GetComponentInParent<DrinkContainer>() != null)
            {
                return;
            }

            currentObject = interactable;

            if (TryGetMousePointOnTabletop(out Vector3 mousePoint))
            {
                dragOffset = currentObject.transform.position - mousePoint;
                dragOffset.y = 0f;
            }
            else
            {
                dragOffset = Vector3.zero;
            }
        }

        private void DragCurrentObject()
        {
            if (!TryGetMousePointOnTabletop(out Vector3 mousePoint))
            {
                return;
            }

            Vector3 targetPosition = mousePoint + dragOffset;
            targetPosition.y = tabletopY;
            currentObject.transform.position = targetPosition;
        }

        private bool TryGetMousePointOnTabletop(out Vector3 point)
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

            if (dragPlane.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private void StopDrag()
        {
            currentObject = null;
            dragOffset = Vector3.zero;
        }

        private void OnValidate()
        {
            dragPlane = new Plane(Vector3.up, new Vector3(0f, tabletopY, 0f));
        }
    }
}
