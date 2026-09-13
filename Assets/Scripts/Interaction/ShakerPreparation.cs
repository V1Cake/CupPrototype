using System;
using System.Collections;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using UnityEngine;

namespace CupPrototype.Interaction
{
    public enum ShakerState { Rest, Preparing, ReadyToShake, ShakeComplete }

    [DisallowMultipleComponent]
    public sealed class ShakerPreparation : MonoBehaviour
    {
        [SerializeField] private Transform prepPosition;
        [SerializeField] private CurrentDrinkRecord currentDrinkRecord;
        [SerializeField] private GameObject processIceFeedback;
        [SerializeField] private Transform lid;
        [SerializeField] private Vector3 openLidLocalPosition;
        [SerializeField] private Transform shakePosition;
        private FlairableTool shakingTool;
        private Vector3 closedLidLocalPosition;
        private Coroutine closing;
        private Action closeCompleted;
        private Coroutine opening;
        private Action openCompleted;
        private Coroutine pouring;
        private Action<bool> pourCompleted;
        private DrinkContainer pourTarget;
        public bool HasShakeIngredients
        {
            get
            {
                var container = GetComponent<DrinkContainer>();
                IngredientData first = null;
                if (!container) return false;
                foreach (var entry in container.Ingredients)
                {
                    if (!entry.ingredient || entry.amount <= 0) continue;
                    if (first && entry.ingredient != first) return true;
                    first = entry.ingredient;
                }
                return false;
            }
        }
        public bool TryServeHold()
        {
            if (!isActiveAndEnabled || State != ShakerState.ShakeComplete || !shakePosition || pouring != null) return false;
            transform.SetPositionAndRotation(shakePosition.position, shakePosition.rotation);
            return true;
        }

        public void ReturnServeHold()
        {
            if (prepPosition) transform.SetPositionAndRotation(prepPosition.position, prepPosition.rotation);
        }

        public bool TryPour(DrinkContainer target, Action<bool> completed)
        {
            var source = GetComponent<DrinkContainer>();
            if (!isActiveAndEnabled || State != ShakerState.ShakeComplete || pouring != null ||
                !lid || !source || !source.isActiveAndEnabled || !target || !target.isActiveAndEnabled ||
                target.containerType != DrinkContainer.ContainerType.FinalGlass || target.CurrentVolume > 0 ||
                target.IsFull() || !target.CanReceiveFrom(source, out _)) return false;
            var body = GetComponentInChildren<Collider>();
            var cup = target.GetComponentInChildren<Collider>();
            if (!body || !cup) return false;
            var mouth = transform.InverseTransformPoint(new Vector3(body.bounds.center.x, body.bounds.max.y, body.bounds.center.z));
            var rotation = Quaternion.AngleAxis(110, Vector3.forward);
            var spout = new Vector3(cup.bounds.center.x, cup.bounds.max.y + .06f, cup.bounds.center.z);
            var pose = new Pose(spout - rotation * Vector3.Scale(mouth, transform.lossyScale), rotation);
            pourTarget = target;
            pourCompleted = completed;
            pouring = StartCoroutine(Pour(source, target, pose));
            return true;
        }

        private IEnumerator Pour(DrinkContainer source, DrinkContainer target, Pose pose)
        {
            // A small lid gap represents straining; it does not reopen preparation.
            lid.localPosition = Vector3.Lerp(closedLidLocalPosition, openLidLocalPosition, .15f);
            yield return MoveJigger(transform, new Pose(transform.position, transform.rotation), pose);
            while (source && target && target.isActiveAndEnabled && !source.IsEmpty() && !target.IsFull())
            {
                if (Time.deltaTime <= 0) { yield return null; continue; }
                if (!source.TransferTo(target, 60f * Time.deltaTime)) break;
                yield return null;
            }
            bool success = source && target && target.isActiveAndEnabled && (source.IsEmpty() || target.IsFull());
            yield return MoveJigger(transform, new Pose(transform.position, transform.rotation), restPose);
            if (source) source.Clear(); // Any remainder becomes Waste only on return.
            pouring = null;
            pourTarget = null;
            ResetToRest();
            var completed = pourCompleted;
            pourCompleted = null;
            completed?.Invoke(success);
        }

        public void CancelPour()
        {
            if (pouring == null) return;
            StopCoroutine(pouring);
            pouring = null;
            if (pourTarget) pourTarget.Clear();
            pourTarget = null;
            GetComponent<DrinkContainer>()?.Clear();
            ResetToRest();
            var completed = pourCompleted;
            pourCompleted = null;
            completed?.Invoke(false);
        }
        public bool CanClose => isActiveAndEnabled && State == ShakerState.Preparing && lid && transfer == null && closing == null && opening == null;
        public bool CanOpen => isActiveAndEnabled &&
            (State == ShakerState.ReadyToShake || State == ShakerState.ShakeComplete) &&
            currentDrinkRecord && lid && prepPosition &&
            transfer == null && closing == null && opening == null && !shakingTool;
        public bool ProcessIce => currentDrinkRecord && currentDrinkRecord.ProcessIce;
        private Pose restPose;
        private Coroutine transfer;
        private DrinkContainer transferringJigger;
        private Pose jiggerHoldPose;
        private Action transferCompleted;
        public ShakerState State { get; private set; } = ShakerState.Rest;

        private void Awake()
        {
            restPose = new Pose(transform.position, transform.rotation);
            if (lid) closedLidLocalPosition = lid.localPosition;
            if (currentDrinkRecord) currentDrinkRecord.Changed += RefreshProcessIceFeedback;
            RefreshProcessIceFeedback();
        }

        private void OnDestroy()
        {
            if (currentDrinkRecord) currentDrinkRecord.Changed -= RefreshProcessIceFeedback;
        }

        private void RefreshProcessIceFeedback()
        {
            if (processIceFeedback) processIceFeedback.SetActive(ProcessIce);
        }

        public bool TryAcquire()
        {
            if (!isActiveAndEnabled || !prepPosition || State != ShakerState.Rest) return false;
            transform.SetPositionAndRotation(prepPosition.position, prepPosition.rotation);
            if (lid) lid.localPosition = openLidLocalPosition;
            State = ShakerState.Preparing;
            return true;
        }

        public bool TryClose(Action completed)
        {
            if (!CanClose) return false;
            closeCompleted = completed;
            closing = StartCoroutine(Close());
            return true;
        }

        public bool TryShake(FlairableTool tool, FlairActionDefinition action, Action<bool> completed)
        {
            if (!isActiveAndEnabled || State != ShakerState.ReadyToShake || !HasShakeIngredients || shakingTool || opening != null ||
                !shakePosition || !prepPosition || !currentDrinkRecord || !currentDrinkRecord.isActiveAndEnabled ||
                !tool || tool.gameObject != gameObject) return false;
            shakingTool = tool;
            transform.SetPositionAndRotation(shakePosition.position, shakePosition.rotation);
            if (tool.TryPlayFlair(action, succeeded =>
            {
                shakingTool = null;
                transform.SetPositionAndRotation(prepPosition.position, prepPosition.rotation);
                if (succeeded)
                {
                    State = ShakerState.ShakeComplete;
                    currentDrinkRecord.RecordShake();
                    TryServeHold();
                }
                completed?.Invoke(succeeded);
            })) return true;
            shakingTool = null;
            transform.SetPositionAndRotation(prepPosition.position, prepPosition.rotation);
            return false;
        }

        public void CancelShake()
        {
            if (shakingTool) shakingTool.CancelFlair();
        }

        public bool TryOpen(Action completed)
        {
            if (!CanOpen) return false;
            openCompleted = completed;
            opening = StartCoroutine(Open());
            return true;
        }

        private IEnumerator Open()
        {
            transform.SetPositionAndRotation(prepPosition.position, prepPosition.rotation);
            var start = lid.localPosition;
            for (float elapsed = 0; elapsed < .35f && lid; elapsed += Time.deltaTime)
            {
                lid.localPosition = Vector3.Lerp(start, openLidLocalPosition, Mathf.SmoothStep(0, 1, elapsed / .35f));
                yield return null;
            }
            if (lid)
            {
                lid.localPosition = openLidLocalPosition;
                State = ShakerState.Preparing;
            }
            FinishOpen();
        }

        private void FinishOpen()
        {
            opening = null;
            var completed = openCompleted;
            openCompleted = null;
            completed?.Invoke();
        }

        public void CancelOpen()
        {
            if (opening == null) return;
            StopCoroutine(opening);
            if (lid) lid.localPosition = closedLidLocalPosition;
            FinishOpen();
        }

        private IEnumerator Close()
        {
            var start = lid.localPosition;
            for (float elapsed = 0; elapsed < .35f && lid; elapsed += Time.deltaTime)
            {
                lid.localPosition = Vector3.Lerp(start, closedLidLocalPosition, Mathf.SmoothStep(0, 1, elapsed / .35f));
                yield return null;
            }
            if (lid)
            {
                lid.localPosition = closedLidLocalPosition;
                State = currentDrinkRecord && currentDrinkRecord.ShakePerformed
                    ? ShakerState.ShakeComplete : ShakerState.ReadyToShake;
            }
            closing = null;
            var completed = closeCompleted;
            closeCompleted = null;
            completed?.Invoke();
        }

        public void CancelClose()
        {
            if (closing == null) return;
            StopCoroutine(closing);
            closing = null;
            if (lid) lid.localPosition = openLidLocalPosition;
            var completed = closeCompleted;
            closeCompleted = null;
            completed?.Invoke();
        }

        public bool TryAddProcessIce()
        {
            if (!isActiveAndEnabled || State != ShakerState.Preparing || transfer != null || closing != null ||
                !currentDrinkRecord || !currentDrinkRecord.TryRecordProcessIce()) return false;
            return true;
        }

        public bool TryTransferFrom(DrinkContainer source, Action completed)
        {
            var target = GetComponent<DrinkContainer>();
            if (!isActiveAndEnabled || State != ShakerState.Preparing || transfer != null || closing != null ||
                !source || !source.isActiveAndEnabled || source.containerType != DrinkContainer.ContainerType.Jigger ||
                source.IsEmpty() || !target || !target.isActiveAndEnabled || !target.UnlimitedReceive ||
                !target.CanReceiveFrom(source, out _)) return false;
            var sourceCollider = source.GetComponentInChildren<Collider>();
            var targetCollider = GetComponentInChildren<Collider>();
            if (!sourceCollider || !targetCollider) return false;
            transferringJigger = source;
            jiggerHoldPose = new Pose(source.transform.position, source.transform.rotation);
            transferCompleted = completed;
            var mouth = source.transform.InverseTransformPoint(new Vector3(sourceCollider.bounds.center.x, sourceCollider.bounds.max.y, sourceCollider.bounds.center.z));
            var rotation = Quaternion.AngleAxis(110, Vector3.forward);
            var spout = new Vector3(targetCollider.bounds.center.x, targetCollider.bounds.max.y + .03f, targetCollider.bounds.center.z);
            var pour = new Pose(spout - rotation * Vector3.Scale(mouth, source.transform.lossyScale), rotation);
            transfer = StartCoroutine(Transfer(source, target, pour));
            return true;
        }

        private IEnumerator Transfer(DrinkContainer source, DrinkContainer target, Pose pour)
        {
            yield return MoveJigger(source.transform, jiggerHoldPose, pour);
            if (source && target && source.isActiveAndEnabled && target.isActiveAndEnabled && State == ShakerState.Preparing)
            {
                float before = target.CurrentVolume;
                if (source.TransferTo(target, source.CurrentVolume) && target.CurrentVolume > before &&
                    currentDrinkRecord) currentDrinkRecord.RecordContentChange();
            }
            if (source) yield return MoveJigger(source.transform, pour, jiggerHoldPose);
            FinishTransfer();
        }

        private static IEnumerator MoveJigger(Transform tool, Pose from, Pose to)
        {
            for (float elapsed = 0; elapsed < .25f && tool; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / .25f);
                tool.SetPositionAndRotation(Vector3.Lerp(from.position, to.position, t), Quaternion.Slerp(from.rotation, to.rotation, t));
                yield return null;
            }
            if (tool) tool.SetPositionAndRotation(to.position, to.rotation);
        }

        private void FinishTransfer()
        {
            if (transferringJigger) transferringJigger.transform.SetPositionAndRotation(jiggerHoldPose.position, jiggerHoldPose.rotation);
            transferringJigger = null;
            transfer = null;
            var completed = transferCompleted;
            transferCompleted = null;
            completed?.Invoke();
        }

        public void CancelTransfer()
        {
            if (transfer != null) StopCoroutine(transfer);
            FinishTransfer();
        }

        private void OnDisable() { CancelPour(); CancelTransfer(); CancelClose(); CancelShake(); CancelOpen(); }

        public void ResetAttempt()
        {
            ResetToRest();
            if (currentDrinkRecord) currentDrinkRecord.ResetAttempt();
        }

        public void ResetToRest()
        {
            CancelPour();
            CancelOpen();
            CancelShake();
            CancelTransfer();
            CancelClose();
            if (lid) lid.localPosition = closedLidLocalPosition;
            transform.SetPositionAndRotation(restPose.position, restPose.rotation);
            State = ShakerState.Rest;
        }
    }
}
