using UnityEngine;

namespace CupPrototype.Interaction
{
    [DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public sealed class MeasurementCameraLock : MonoBehaviour
    {
        private Pose pose;
        public bool IsLocked { get; private set; }
        public void Lock()
        {
            if (IsLocked) return;
            pose = new Pose(transform.position, transform.rotation);
            IsLocked = true;
        }
        public void Unlock() => IsLocked = false;
        private void LateUpdate()
        {
            if (IsLocked) transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
        private void OnDisable() => Unlock();
    }
}
