using System.Collections.Generic;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.UI;
using CupPrototype.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CupPrototype.Interaction
{
    public enum ActionState
    {
        Stable,
        Measurement,
        AutoTransfer,
        Flair,
        Shake,
        AutoPour,
        Closing,
        Opening,
        Tasting
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public sealed class InteractionCoordinator : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform bottleHoldAnchor;
        [SerializeField] private DrinkContainer jigger;
        [SerializeField] private Transform jiggerHoldAnchor;
        [SerializeField] private MeasurementOverlay measurementOverlay;
        [SerializeField] private MeasurementCameraLock measurementCamera;
        [SerializeField] private ShakerPreparation shaker;
        [SerializeField] private Transform iceWell;
        [SerializeField] private Transform serveIceWell;
        [SerializeField] private StirStickTaste stirStick;
        [SerializeField] private DrinkContainer[] rackCups = System.Array.Empty<DrinkContainer>();
        [SerializeField] private Transform servePosition;
        [SerializeField] private Transform finalHoldAnchor;
        [SerializeField] private Collider serviceZone;
        [SerializeField] private Transform servicePosition;
        [SerializeField] private DemoRoundManager roundManager;
        private bool IsSubmitted => GetComponent<CurrentDrinkRecord>() is { Submitted: true };
        private readonly Dictionary<DrinkContainer, Pose> cupRestPoses = new();
        private readonly Dictionary<DrinkContainer, Vector3> cupBottomOffsets = new();
        [SerializeField] private LayerMask bottleLayers = ~0;
        private DrinkTestManager legacyInteraction;
        private readonly Dictionary<PourableIngredient, Pose> bottleRestPoses = new();
        private Pose jiggerRestPose;
        private bool heldPress;
        private int consumedFrame = -1;
        private PourableIngredient measurementBottle;
        private Pose measurementPose;
        private FlairableTool shakeGestureTool;

        public bool OwnsHeldInput => isActiveAndEnabled &&
            (IsSubmitted || CurrentActionState != ActionState.Stable || PrimaryHeld != null || SecondaryHeld != null || heldPress || consumedFrame == Time.frameCount);
        public GameObject Selected { get; private set; }
        public GameObject PrimaryHeld { get; private set; }
        public GameObject SecondaryHeld { get; private set; }
        public ActionState CurrentActionState { get; private set; } = ActionState.Stable;

        private void Awake()
        {
            ClearState();
            if (!targetCamera) targetCamera = Camera.main;
            legacyInteraction = GetComponent<DrinkTestManager>();
            foreach (var bottle in FindObjectsByType<PourableIngredient>())
                bottleRestPoses[bottle] = new Pose(bottle.transform.position, bottle.transform.rotation);
            if (jigger) jiggerRestPose = new Pose(jigger.transform.position, jigger.transform.rotation);
            foreach (var cup in rackCups)
            {
                if (!cup || !TryGetVisibleBounds(cup.gameObject, out var bounds)) continue;
                cupRestPoses[cup] = new Pose(cup.transform.position, cup.transform.rotation);
                cupBottomOffsets[cup] = Quaternion.Inverse(cup.transform.rotation) *
                    (new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - cup.transform.position);
            }
        }

        private bool CanUseHeldInput => isActiveAndEnabled && !IsSubmitted && CurrentActionState == ActionState.Stable &&
            (!legacyInteraction || legacyInteraction.enabled) &&
            !FlairGestureController.IsFlairInputActive && !FlairGestureController.IsFlairPlaying &&
            !GestureTemplateRecorder.IsTemplateRecording;

        private void Update()
        {
            if (!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0)) heldPress = false;
            if (CurrentActionState == ActionState.Measurement)
            {
                AdvanceMeasurement(Input.GetMouseButton(0), Time.deltaTime);
                return;
            }
            if (!CanUseHeldInput || (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
            if (Input.GetMouseButtonDown(1)) TryReturnHeld();
            if (!Input.GetMouseButtonDown(0) || !targetCamera) return;
            var ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f, bottleLayers))
            {
                if (hit.collider == serviceZone)
                {
                    heldPress = true; consumedFrame = Time.frameCount;
                    TrySubmitCup(hit.collider);
                    return;
                }
                if (stirStick && hit.collider.GetComponentInParent<StirStickTaste>() == stirStick)
                {
                    heldPress = true;
                    consumedFrame = Time.frameCount;
                    TryTaste(hit.collider);
                    return;
                }
                if (serveIceWell && hit.collider.transform.IsChildOf(serveIceWell))
                {
                    heldPress = true;
                    consumedFrame = Time.frameCount;
                    TryAddServeIce(hit.collider);
                    return;
                }
                if (iceWell && hit.collider.transform.IsChildOf(iceWell))
                {
                    heldPress = true;
                    consumedFrame = Time.frameCount;
                    TryAddProcessIce(hit.collider);
                    return;
                }
                var bottle = hit.collider.GetComponentInParent<PourableIngredient>();
                var container = hit.collider.GetComponentInParent<DrinkContainer>();
                if (container && cupRestPoses.ContainsKey(container))
                {
                    heldPress = true;
                    consumedFrame = Time.frameCount;
                    if (GetComponent<CurrentDrinkRecord>().PourCompleted) TryAcquireFinishedCup(container);
                    else if (shaker && PrimaryHeld == shaker.gameObject && GetComponent<CurrentDrinkRecord>().SelectedGlass == container) TryStartAutoPour(container);
                    else TryAcquireCup(container);
                    return;
                }
                var clickedShaker = hit.collider.GetComponentInParent<ShakerPreparation>();
                if (clickedShaker && clickedShaker == shaker)
                {
                    heldPress = true;
                    consumedFrame = Time.frameCount;
                    if (clickedShaker.State == ShakerState.ShakeComplete) TryAcquireServeShaker(clickedShaker);
                    else if (jigger && (PrimaryHeld == jigger.gameObject || SecondaryHeld == jigger.gameObject)) TryStartJiggerTransfer(clickedShaker);
                    else TryAcquireShaker(clickedShaker);
                    return;
                }
                if (!bottle && (!container || container.containerType != DrinkContainer.ContainerType.Jigger)) return;
                heldPress = true;
                consumedFrame = Time.frameCount;
                if (bottle) TryAcquireBottle(bottle);
                else if (SecondaryHeld == container.gameObject) TryStartMeasurement(ray);
                else TryAcquireJigger(container);
            }
        }

        private void LateUpdate()
        {
            if (PrimaryHeld && finalHoldAnchor && PrimaryHeld.TryGetComponent<DrinkContainer>(out var cup) && cupRestPoses.ContainsKey(cup))
                PlaceCup(cup, finalHoldAnchor);
            if (PrimaryHeld && bottleHoldAnchor && PrimaryHeld.TryGetComponent<PourableIngredient>(out _))
            {
                var pose = CurrentActionState == ActionState.Measurement
                    ? measurementPose : new Pose(bottleHoldAnchor.position, bottleHoldAnchor.rotation);
                PrimaryHeld.transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
            if (CurrentActionState != ActionState.AutoTransfer && jigger && jiggerHoldAnchor && (PrimaryHeld == jigger.gameObject || SecondaryHeld == jigger.gameObject))
                jigger.transform.SetPositionAndRotation(jiggerHoldAnchor.position, jiggerHoldAnchor.rotation);
        }

        public bool CanCloseShaker => CanUseHeldInput && shaker && shaker.CanClose;

        private void PlaceCup(DrinkContainer cup, Transform anchor) => cup.transform.SetPositionAndRotation(
            anchor.position - anchor.rotation * cupBottomOffsets[cup], anchor.rotation);

        public bool TryAcquireFinishedCup(DrinkContainer cup)
        {
            var record = GetComponent<CurrentDrinkRecord>();
            if (!CanUseHeldInput || PrimaryHeld || SecondaryHeld || !finalHoldAnchor || !record ||
                !record.PourCompleted || !cup || !cup.isActiveAndEnabled || cup.CurrentVolume <= 0 ||
                record.SelectedGlass != cup || !cupRestPoses.ContainsKey(cup)) return false;
            PrimaryHeld = Selected = cup.gameObject;
            PlaceCup(cup, finalHoldAnchor);
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TrySubmitCup(Collider clicked)
        {
            var record = GetComponent<CurrentDrinkRecord>();
            var cup = record ? record.SelectedGlass : null;
            if (!CanUseHeldInput || !serviceZone || clicked != serviceZone || !clicked.enabled ||
                !clicked.gameObject.activeInHierarchy || !servicePosition || !roundManager || !cup ||
                PrimaryHeld != cup.gameObject || SecondaryHeld || !cupRestPoses.ContainsKey(cup)) return false;
            if (!roundManager.TrySubmitFinishedDrink(record.ActiveOrder, record, cup)) return false;
            PlaceCup(cup, servicePosition);
            PrimaryHeld = SecondaryHeld = Selected = null;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryAcquireServeShaker(ShakerPreparation target)
        {
            var record = GetComponent<CurrentDrinkRecord>();
            if (!CanUseHeldInput || !shaker || target != shaker || PrimaryHeld || SecondaryHeld ||
                !record || record.PourCompleted || !record.SelectedGlass || record.SelectedGlass.CurrentVolume > 0 ||
                !shaker.TryServeHold()) return false;
            Selected = PrimaryHeld = shaker.gameObject;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryStartAutoPour(DrinkContainer cup)
        {
            var record = GetComponent<CurrentDrinkRecord>();
            if (!CanUseHeldInput || !shaker || PrimaryHeld != shaker.gameObject || SecondaryHeld ||
                !record || !record.isActiveAndEnabled || record.PourCompleted || !cup || record.SelectedGlass != cup ||
                cup.CurrentVolume > 0 || shaker.State != ShakerState.ShakeComplete) return false;
            CurrentActionState = ActionState.AutoPour;
            if (!shaker.TryPour(cup, success =>
            {
                PrimaryHeld = SecondaryHeld = null;
                Selected = success ? cup.gameObject : null;
                if (success && record) record.RecordPour();
                CurrentActionState = ActionState.Stable;
                consumedFrame = Time.frameCount;
            }))
            {
                CurrentActionState = ActionState.Stable;
                return false;
            }
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryAcquireCup(DrinkContainer cup)
        {
            var record = GetComponent<CurrentDrinkRecord>();
            if (!CanUseHeldInput ||
                !servePosition || !record || !record.isActiveAndEnabled || record.PourCompleted || !cup || !cup.isActiveAndEnabled ||
                cup.containerType != DrinkContainer.ContainerType.FinalGlass || !cupRestPoses.ContainsKey(cup) ||
                record.SelectedGlass == cup || (record.SelectedGlass && record.SelectedGlass.CurrentVolume > 0f)) return false;
            ReturnServeCup();
            cup.transform.SetPositionAndRotation(servePosition.position - servePosition.rotation * cupBottomOffsets[cup], servePosition.rotation);
            record.SelectGlass(cup);
            Selected = cup.gameObject;
            consumedFrame = Time.frameCount;
            return true;
        }

        private void ReturnServeCup()
        {
            var record = GetComponent<CurrentDrinkRecord>();
            var cup = record ? record.SelectedGlass : null;
            if (cup && PrimaryHeld == cup.gameObject) PrimaryHeld = null;
            if (record && cup) record.SelectGlass(null);
            if (cup && cupRestPoses.TryGetValue(cup, out var rest))
            {
                cup.transform.SetPositionAndRotation(rest.position, rest.rotation);
                if (Selected == cup.gameObject) Selected = null;
            }
        }

        public bool TryTaste(Collider clicked)
        {
            var record = GetComponent<CurrentDrinkRecord>();
            var container = shaker ? shaker.GetComponent<DrinkContainer>() : null;
            if (!CanUseHeldInput || !shaker || !shaker.isActiveAndEnabled || shaker.State != ShakerState.Preparing ||
                !container || !container.isActiveAndEnabled || container.IsEmpty() || !record || !record.isActiveAndEnabled ||
                !record.CanTasteCurrentVersion || record.ActiveOrder == null || !record.ActiveOrder.Recipe || !stirStick || !clicked || !clicked.enabled ||
                !clicked.gameObject.activeInHierarchy || clicked.GetComponentInParent<StirStickTaste>() != stirStick) return false;
            var order = record.ActiveOrder;
            CurrentActionState = ActionState.Tasting;
            if (!stirStick.TryPlay(success =>
            {
                if (success && record && ReferenceEquals(record.ActiveOrder, order) &&
                    shaker && shaker.State == ShakerState.Preparing && container && !container.IsEmpty())
                    record.RecordTaste(TasteFeedbackSystem.GenerateFeedback(container, order.Recipe));
                CurrentActionState = ActionState.Stable;
                consumedFrame = Time.frameCount;
            }))
            {
                CurrentActionState = ActionState.Stable;
                return false;
            }
            consumedFrame = Time.frameCount;
            return true;
        }
        public bool CanOpenShaker => CanUseHeldInput && shaker && PrimaryHeld != shaker.gameObject && shaker.CanOpen;

        public bool TryOpenShaker()
        {
            if (!CanOpenShaker) return false;
            CurrentActionState = ActionState.Opening;
            if (!shaker.TryOpen(() => { CurrentActionState = ActionState.Stable; consumedFrame = Time.frameCount; }))
            {
                CurrentActionState = ActionState.Stable;
                return false;
            }
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryBeginShakeGesture(FlairableTool tool)
        {
            if (!CanUseHeldInput || PrimaryHeld || SecondaryHeld || !shaker || !shaker.isActiveAndEnabled ||
                shaker.State != ShakerState.ReadyToShake || !tool || !tool.isActiveAndEnabled ||
                tool.gameObject != shaker.gameObject || !tool.enableFlair || !tool.visualRoot) return false;
            if (!CheckShakeIngredients()) return false;
            shakeGestureTool = tool;
            Selected = shaker.gameObject;
            CurrentActionState = ActionState.Flair;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool CompleteShakeGesture(GestureMatchResult match)
        {
            var tool = shakeGestureTool;
            CancelShakeGesture();
            if (!CanUseHeldInput || !tool || !shaker || shaker.State != ShakerState.ReadyToShake ||
                !tool.TryGetAction(match, out var action)) return false;
            if (!CheckShakeIngredients()) return false;
            CurrentActionState = ActionState.Shake;
            PrimaryHeld = shaker.gameObject;
            if (shaker.TryShake(tool, action, succeeded =>
            {
                PrimaryHeld = succeeded ? shaker.gameObject : null;
                SecondaryHeld = null;
                CurrentActionState = ActionState.Stable;
                consumedFrame = Time.frameCount;
            })) return true;
            PrimaryHeld = null;
            CurrentActionState = ActionState.Stable;
            return false;
        }

        private bool CheckShakeIngredients()
        {
            if (shaker.HasShakeIngredients) return true;
            DemoMessagePanel.Instance?.ShowMessage("Not enough ingredients to shake.");
            return false;
        }

        public void CancelShakeGesture()
        {
            if (!shakeGestureTool) return;
            shakeGestureTool = null;
            CurrentActionState = ActionState.Stable;
            consumedFrame = Time.frameCount;
        }

        public bool TryCloseShaker()
        {
            if (!CanCloseShaker) return false;
            CurrentActionState = ActionState.Closing;
            if (!shaker.TryClose(() => { CurrentActionState = ActionState.Stable; consumedFrame = Time.frameCount; }))
            {
                CurrentActionState = ActionState.Stable;
                return false;
            }
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryAddProcessIce(Collider target)
        {
            if (!CanUseHeldInput || !iceWell || !target || !target.enabled ||
                !target.gameObject.activeInHierarchy || !target.transform.IsChildOf(iceWell) ||
                !shaker || !shaker.TryAddProcessIce()) return false;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryAddServeIce(Collider target)
        {
            if (!CanUseHeldInput || !serveIceWell || !target || !target.enabled ||
                !target.gameObject.activeInHierarchy || !target.transform.IsChildOf(serveIceWell)) return false;
            var record = GetComponent<CurrentDrinkRecord>();
            if (!record || !record.TryRecordServeIce()) return false;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryStartJiggerTransfer(ShakerPreparation target)
        {
            bool primaryJigger = jigger && PrimaryHeld == jigger.gameObject && !SecondaryHeld;
            bool secondaryJigger = jigger && SecondaryHeld == jigger.gameObject && PrimaryHeld &&
                PrimaryHeld.TryGetComponent<PourableIngredient>(out _);
            if (!CanUseHeldInput || !shaker || target != shaker || (!primaryJigger && !secondaryJigger))
                return false;
            CurrentActionState = ActionState.AutoTransfer;
            if (!shaker.TryTransferFrom(jigger, () => { CurrentActionState = ActionState.Stable; consumedFrame = Time.frameCount; }))
            {
                CurrentActionState = ActionState.Stable;
                return false;
            }
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryAcquireShaker(ShakerPreparation target)
        {
            if (!CanUseHeldInput || !shaker || target != shaker || PrimaryHeld || SecondaryHeld ||
                !shaker.TryAcquire()) return false;
            Selected = shaker.gameObject;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryAcquireBottle(PourableIngredient bottle)
        {
            if (!CanUseHeldInput || (PrimaryHeld && (!jigger || PrimaryHeld != jigger.gameObject)) ||
                SecondaryHeld || !bottleHoldAnchor || !bottle ||
                !bottle.isActiveAndEnabled || !bottleRestPoses.ContainsKey(bottle)) return false;
            if (PrimaryHeld) SecondaryHeld = PrimaryHeld;
            bottle.transform.SetPositionAndRotation(bottleHoldAnchor.position, bottleHoldAnchor.rotation);
            Selected = PrimaryHeld = bottle.gameObject;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryStartMeasurement(Ray aim)
        {
            if (!CanUseHeldInput || !targetCamera || !jigger || SecondaryHeld != jigger.gameObject || !PrimaryHeld ||
                !PrimaryHeld.TryGetComponent<PourableIngredient>(out var bottle) || !bottle.ingredientData ||
                bottle.pourRatePerSecond <= 0f || jigger.IsFull() || !jigger.CanAcceptIngredient(bottle.ingredientData) ||
                !measurementOverlay || !measurementOverlay.isActiveAndEnabled ||
                !measurementCamera || !measurementCamera.isActiveAndEnabled ||
                !Physics.Raycast(aim, out var hit, 1000f, bottleLayers) ||
                hit.collider.GetComponentInParent<DrinkContainer>() != jigger) return false;

            if (!TryGetVisibleBounds(bottle.gameObject, out var bottleBounds) ||
                !TryGetVisibleBounds(jigger.gameObject, out var jiggerBounds)) return false;
            var mouth = bottle.transform.InverseTransformPoint(new Vector3(bottleBounds.center.x, bottleBounds.max.y, bottleBounds.center.z));
            var rotation = Quaternion.FromToRotation(Vector3.up, (-targetCamera.transform.right * .85f + Vector3.down * .53f).normalized);
            var spout = new Vector3(jiggerBounds.center.x, jiggerBounds.max.y + .04f, jiggerBounds.center.z);
            measurementPose = new Pose(spout - rotation * Vector3.Scale(mouth, bottle.transform.lossyScale), rotation);
            measurementBottle = bottle;
            CurrentActionState = ActionState.Measurement;
            heldPress = true;
            consumedFrame = Time.frameCount;
            measurementCamera.Lock();
            measurementOverlay.Show(jigger, bottle.ingredientData);
            bottle.transform.SetPositionAndRotation(measurementPose.position, measurementPose.rotation);
            return true;
        }

        public void AdvanceMeasurement(bool leftHeld, float deltaTime)
        {
            if (CurrentActionState != ActionState.Measurement) return;
            if (!leftHeld || !measurementBottle || PrimaryHeld != measurementBottle.gameObject ||
                !jigger || !jigger.isActiveAndEnabled || SecondaryHeld != jigger.gameObject ||
                !measurementOverlay || !measurementOverlay.isActiveAndEnabled ||
                !measurementCamera || !measurementCamera.isActiveAndEnabled ||
                jigger.IsFull() || !jigger.CanAcceptIngredient(measurementBottle.ingredientData))
            {
                EndMeasurement();
                return;
            }
            float amount = measurementBottle.pourRatePerSecond * Mathf.Max(0f, deltaTime);
            if (amount > 0f) jigger.AddIngredient(measurementBottle.ingredientData, amount, false);
            if (jigger.IsFull()) EndMeasurement();
        }

        public void EndMeasurement()
        {
            if (CurrentActionState != ActionState.Measurement) return;
            if (measurementBottle && bottleHoldAnchor)
                measurementBottle.transform.SetPositionAndRotation(bottleHoldAnchor.position, bottleHoldAnchor.rotation);
            measurementBottle = null;
            if (measurementOverlay) measurementOverlay.Hide();
            if (measurementCamera) measurementCamera.Unlock();
            CurrentActionState = ActionState.Stable;
            consumedFrame = Time.frameCount;
        }

        private static bool TryGetVisibleBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (found) bounds.Encapsulate(renderer.bounds);
                else { bounds = renderer.bounds; found = true; }
            }
            return found;
        }

        private void OnDisable()
        {
            if (shaker) shaker.CancelPour();
            if (stirStick) stirStick.Cancel();
            if (shaker) shaker.CancelOpen();
            GetComponent<FlairGestureController>()?.CancelRecording();
            if (shaker) shaker.CancelShake();
            EndMeasurement();
            if (shaker) shaker.CancelTransfer();
            if (shaker) shaker.CancelClose();
        }

        public bool TryReturnBottle()
        {
            if (!CanUseHeldInput || !PrimaryHeld ||
                !PrimaryHeld.TryGetComponent<PourableIngredient>(out var bottle) ||
                !bottleRestPoses.TryGetValue(bottle, out var rest)) return false;
            bottle.transform.SetPositionAndRotation(rest.position, rest.rotation);
            PrimaryHeld = SecondaryHeld;
            SecondaryHeld = null;
            Selected = PrimaryHeld;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryAcquireJigger(DrinkContainer target)
        {
            if (!CanUseHeldInput || !jigger || target != jigger || !jigger.isActiveAndEnabled ||
                jigger.containerType != DrinkContainer.ContainerType.Jigger || !jiggerHoldAnchor || SecondaryHeld ||
                (PrimaryHeld && !PrimaryHeld.TryGetComponent<PourableIngredient>(out _))) return false;
            jigger.transform.SetPositionAndRotation(jiggerHoldAnchor.position, jiggerHoldAnchor.rotation);
            if (PrimaryHeld) SecondaryHeld = jigger.gameObject;
            else PrimaryHeld = jigger.gameObject;
            Selected = jigger.gameObject;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool TryReturnHeld()
        {
            if (CanUseHeldInput && PrimaryHeld && PrimaryHeld.TryGetComponent<DrinkContainer>(out var cup) && cupRestPoses.ContainsKey(cup))
            {
                PlaceCup(cup, servePosition);
                PrimaryHeld = Selected = null;
                consumedFrame = Time.frameCount;
                return true;
            }
            if (CanUseHeldInput && shaker && PrimaryHeld == shaker.gameObject)
            {
                shaker.ReturnServeHold();
                PrimaryHeld = Selected = null;
                consumedFrame = Time.frameCount;
                return true;
            }
            if (TryReturnBottle()) return true;
            if (!CanUseHeldInput || !jigger || PrimaryHeld != jigger.gameObject) return false;
            jigger.transform.SetPositionAndRotation(jiggerRestPose.position, jiggerRestPose.rotation);
            PrimaryHeld = null;
            Selected = null;
            consumedFrame = Time.frameCount;
            return true;
        }

        public void SetSelected(GameObject selected) => Selected = selected;

        public void SetHeld(GameObject primary, GameObject secondary)
        {
            if (CurrentActionState != ActionState.Stable) return;
            if (primary && primary != (jigger ? jigger.gameObject : null) &&
                !primary.TryGetComponent<PourableIngredient>(out _)) return;
            if (secondary && (!jigger || secondary != jigger.gameObject || !primary ||
                !primary.TryGetComponent<PourableIngredient>(out _))) return;
            PrimaryHeld = primary;
            SecondaryHeld = secondary;
        }

        public void SetActionState(ActionState state)
        {
            if (CurrentActionState == ActionState.AutoPour && shaker) shaker.CancelPour();
            if (CurrentActionState == ActionState.Tasting && stirStick) stirStick.Cancel();
            if (CurrentActionState == ActionState.Opening && shaker) shaker.CancelOpen();
            if (shakeGestureTool) GetComponent<FlairGestureController>()?.CancelRecording();
            if (CurrentActionState == ActionState.Shake && shaker) shaker.CancelShake();
            if (CurrentActionState == ActionState.Closing && shaker) shaker.CancelClose();
            if (CurrentActionState == ActionState.AutoTransfer && shaker) shaker.CancelTransfer();
            if (CurrentActionState == ActionState.Measurement) EndMeasurement();
            if (state != ActionState.Measurement) CurrentActionState = state;
        }

        public void ResetPreparation()
        {
            DemoMessagePanel.Instance?.ClearMessage();
            if (shaker) shaker.CancelPour();
            if (shaker && PrimaryHeld == shaker.gameObject) PrimaryHeld = Selected = null;
            ReturnServeCup();
            if (stirStick) stirStick.Cancel();
            GetComponent<FlairGestureController>()?.CancelRecording();
            EndMeasurement();
            if (shaker) shaker.ResetAttempt();
        }

        public void ClearState()
        {
            if (shaker) shaker.CancelPour();
            ReturnServeCup();
            if (stirStick) stirStick.Cancel();
            if (shaker) shaker.CancelOpen();
            GetComponent<FlairGestureController>()?.CancelRecording();
            if (shaker) shaker.CancelShake();
            if (shaker) shaker.CancelClose();
            if (shaker) shaker.CancelTransfer();
            EndMeasurement();
            Selected = null;
            PrimaryHeld = null;
            SecondaryHeld = null;
            CurrentActionState = ActionState.Stable;
        }
    }
}
