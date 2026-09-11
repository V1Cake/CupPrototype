using System;
using System.Collections;
using UnityEngine;

namespace CupPrototype.Interaction
{
    [DisallowMultipleComponent]
    public sealed class StirStickTaste : MonoBehaviour
    {
        [SerializeField] private Renderer bowl;
        [SerializeField] private Renderer handle;
        [SerializeField] private Renderer[] parts;
        [SerializeField] private Transform dipPoint;
        [SerializeField] private Transform samplePoint;
        private Pose rest;
        private Vector3 localTip;
        private Quaternion upright;
        private Pose pose;
        private Pose[] partRest;
        private Coroutine routine;
        private Action<bool> finished;
        public bool IsPlaying => routine != null;

        private void Awake()
        {
            rest = new Pose(transform.position, transform.rotation);
            pose = rest;
            partRest = new Pose[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                partRest[i] = new Pose(parts[i].transform.position, parts[i].transform.rotation);
            if (bowl && handle)
            {
                localTip = transform.InverseTransformPoint(bowl.bounds.center);
                upright = Quaternion.FromToRotation((handle.bounds.center - bowl.bounds.center).normalized, Vector3.up) * transform.rotation;
            }
        }

        public bool TryPlay(Action<bool> completed)
        {
            if (!isActiveAndEnabled || routine != null || !bowl || !handle || !dipPoint || !samplePoint) return false;
            finished = completed;
            routine = StartCoroutine(Play());
            return true;
        }

        private Pose AtTip(Vector3 tip) => new Pose(tip - upright * Vector3.Scale(localTip, transform.lossyScale), upright);

        private IEnumerator Play()
        {
            var dip = AtTip(dipPoint.position);
            yield return Move(rest, dip, .35f);
            for (float elapsed = 0; elapsed < .65f; elapsed += Time.deltaTime)
            {
                float angle = elapsed / .65f * Mathf.PI * 2;
                var pose = AtTip(dipPoint.position + new Vector3(Mathf.Sin(angle), 0, 1 - Mathf.Cos(angle)) * .012f);
                SetPose(pose);
                yield return null;
            }
            var sample = AtTip(samplePoint.position);
            yield return Move(pose, sample, .4f);
            yield return new WaitForSeconds(.3f);
            yield return Move(sample, rest, .4f);
            Finish(true);
        }

        private IEnumerator Move(Pose from, Pose to, float duration)
        {
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                SetPose(new Pose(Vector3.Lerp(from.position, to.position, t), Quaternion.Slerp(from.rotation, to.rotation, t)));
                yield return null;
            }
            SetPose(to);
        }

        private void SetPose(Pose value)
        {
            pose = value;
            Quaternion delta = value.rotation * Quaternion.Inverse(rest.rotation);
            for (int i = 0; i < parts.Length; i++)
                parts[i].transform.SetPositionAndRotation(value.position + delta * (partRest[i].position - rest.position), delta * partRest[i].rotation);
        }

        public void Cancel()
        {
            if (routine == null) return;
            StopCoroutine(routine);
            Finish(false);
        }
        private void OnDisable() => Cancel();
        private void Finish(bool success)
        {
            SetPose(rest);
            routine = null;
            var callback = finished;
            finished = null;
            callback?.Invoke(success);
        }
    }
}
