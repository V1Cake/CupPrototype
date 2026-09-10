#if UNITY_EDITOR
using System;
using System.Linq;
using CupPrototype.DrinkSystem;
using CupPrototype.Interaction;
using UnityEditor;
using UnityEngine;

public static class InteractionCoordinatorJiggerCheck
{
    public const string ResultKey = "T03.JiggerCheck";
    static InteractionCoordinator coordinator;
    static DrinkContainer jigger;
    static PourableIngredient bottle;
    static Transform anchor;
    static Pose jiggerRest, bottleRest;
    static string jiggerData;
    static int step, lastFrame;
    static double started;
    static bool previousBackground;

    [MenuItem("Tools/CupPrototype/Validate T03 Jigger (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying && !EditorApplication.isPaused, "Run in unpaused Play Mode");
        coordinator = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var serialized = new SerializedObject(coordinator);
        jigger = serialized.FindProperty("jigger").objectReferenceValue as DrinkContainer;
        anchor = serialized.FindProperty("jiggerHoldAnchor").objectReferenceValue as Transform;
        bottle = GameObject.Find("Bottle_Vodka").GetComponent<PourableIngredient>();
        Require(jigger && anchor && bottle, "scene bindings");
        Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld, "start with empty hands");
        jiggerRest = new Pose(jigger.transform.position, jigger.transform.rotation);
        bottleRest = new Pose(bottle.transform.position, bottle.transform.rotation);
        jiggerData = JsonUtility.ToJson(jigger);
        step = 0; lastFrame = -1; started = EditorApplication.timeSinceStartup;
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        SessionState.SetString(ResultKey, "RUNNING");
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("T03: " + message);
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish("FAIL: " + message);
    }

    static void AtPose(Transform target, Pose pose)
    {
        Require(Vector3.Distance(target.position, pose.position) < .000001f && Quaternion.Angle(target.rotation, pose.rotation) < .001f, "exact pose " + target.name);
    }

    static void CheckJiggerVisible()
    {
        AtPose(jigger.transform, new Pose(anchor.position, anchor.rotation));
        var renderers = jigger.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
        Require(renderers.Length > 0, "visible jigger renderer");
        foreach (var r in renderers)
        {
            var bounds = r.bounds;
            for (int k = 0; k < 8; k++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                var v = Camera.main.WorldToViewportPoint(corner);
                Require(v.x > .1f && v.x < .45f && v.y > .1f && v.y < .65f && v.z > Camera.main.nearClipPlane, "left hold viewport bounds");
            }
        }
    }

    static void Tick()
    {
        try
        {
            Require(Application.isPlaying && EditorApplication.timeSinceStartup - started < 40, "play stopped or timeout");
            EditorApplication.QueuePlayerLoopUpdate();
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (step == 0)
            {
                Require(!coordinator.TryAcquireJigger(null), "reject null");
                foreach (var other in UnityEngine.Object.FindObjectsByType<DrinkContainer>().Where(x => x != jigger))
                    Require(!coordinator.TryAcquireJigger(other), "reject nonconfigured tools");
                coordinator.SetHeld(jigger.gameObject, jigger.gameObject);
                Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld, "reject Jigger + Jigger");
                Require(coordinator.TryAcquireJigger(jigger) && coordinator.PrimaryHeld == jigger.gameObject && !coordinator.SecondaryHeld, "solo acquire");
            }
            else if (step == 1)
            {
                CheckJiggerVisible();
                Require(!coordinator.TryAcquireJigger(jigger), "occupied primary cannot duplicate or replace");
                Require(coordinator.TryReturnHeld(), "solo return");
                AtPose(jigger.transform, jiggerRest);
                Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld, "solo cleared");
            }
            else if (step < 10)
            {
                switch ((step - 2) % 4)
                {
                    case 0:
                        Require(coordinator.TryAcquireBottle(bottle), "bottle acquire");
                        break;
                    case 1:
                        Require(coordinator.TryAcquireJigger(jigger), "secondary acquire");
                        Require(coordinator.PrimaryHeld == bottle.gameObject && coordinator.SecondaryHeld == jigger.gameObject, "dual refs");
                        Require(!coordinator.TryAcquireJigger(jigger), "no duplicate secondary");
                        break;
                    case 2:
                        CheckJiggerVisible();
                        coordinator.SetActionState(ActionState.AutoTransfer);
                        Require(!coordinator.TryReturnHeld(), "busy RMB rejected");
                        coordinator.SetActionState(ActionState.Stable);
                        Require(coordinator.TryReturnHeld(), "first RMB");
                        AtPose(bottle.transform, bottleRest);
                        Require(coordinator.PrimaryHeld == jigger.gameObject && !coordinator.SecondaryHeld, "promote jigger without duplicate ref");
                        CheckJiggerVisible();
                        break;
                    case 3:
                        Require(coordinator.TryReturnHeld(), "second RMB");
                        AtPose(jigger.transform, jiggerRest);
                        Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld && !coordinator.Selected, "dual cleared");
                        break;
                }
            }
            else
            {
                Require(coordinator.CurrentActionState == ActionState.Stable && !coordinator.PrimaryHeld && !coordinator.SecondaryHeld && !coordinator.Selected, "final state");
                Require(JsonUtility.ToJson(jigger) == jiggerData, "liquid/capacity data unchanged");
                Require(!coordinator.GetComponent<DragController>().CurrentDraggedObject, "no legacy drag");
                Finish("PASS: solo Jigger; two dual Acquire/Return cycles; promotion; exact poses; left viewport bounds; illegal/busy rejection; no liquid changes or runtime errors");
                return;
            }
            step++;
        }
        catch (Exception error) { Finish("FAIL: " + error.Message); }
    }

    static void Finish(string status)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        Application.runInBackground = previousBackground;
        SessionState.SetString(ResultKey, status);
        Debug.Log("T03_AUTOMATED_" + status);
    }
}
#endif