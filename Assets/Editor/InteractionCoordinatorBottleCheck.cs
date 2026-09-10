#if UNITY_EDITOR
using System;
using System.Linq;
using CupPrototype.DrinkSystem;
using CupPrototype.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InteractionCoordinatorBottleCheck
{
    const string Key = "T02.BottleCheck";
    static double started;
    static int step;
    static int lastFrame = -1;
    static InteractionCoordinator coordinator;
    static PourableIngredient[] bottles;
    static Pose[] rests;
    static Transform anchor;

    static InteractionCoordinatorBottleCheck()
    {
        if (SessionState.GetBool(Key, false)) Hook();
    }

    public static void Run()
    {
        SessionState.SetBool(Key, true);
        Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
    }

    static void Hook()
    {
        started = EditorApplication.timeSinceStartup;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("T02: " + message);
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(1);
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup - started > 90) { Finish(2); return; }
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try
        {
            if (!coordinator)
            {
                var all = UnityEngine.Object.FindObjectsByType<InteractionCoordinator>(FindObjectsInactive.Include);
                Require(all.Length == 1 && all[0].isActiveAndEnabled, "one active coordinator");
                coordinator = all[0];
                var serialized = new SerializedObject(coordinator);
                anchor = serialized.FindProperty("bottleHoldAnchor").objectReferenceValue as Transform;
                Require(anchor && serialized.FindProperty("targetCamera").objectReferenceValue, "scene references");
                bottles = UnityEngine.Object.FindObjectsByType<PourableIngredient>().OrderBy(b => b.name).Take(2).ToArray();
                Require(bottles.Length == 2, "two bottles available");
                rests = bottles.Select(b => new Pose(b.transform.position, b.transform.rotation)).ToArray();
                Require(coordinator.CurrentActionState == ActionState.Stable && !coordinator.PrimaryHeld && !coordinator.SecondaryHeld && !coordinator.Selected, "initial state");
            }
            int round = step / 2;
            if (round == 4)
            {
                Require(!coordinator.TryAcquireBottle(null), "null rejected");
                Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld && !coordinator.Selected, "no state residue");
                Require(!coordinator.GetComponent<DrinkTestManager>().SelectedIngredient, "legacy bottle selection untouched");
                Require(!coordinator.GetComponent<DragController>().CurrentDraggedObject, "no desktop drag");
                Debug.Log("T02_AUTOMATED_PASS: compile; scene references; two bottles x two Acquire/Return cycles; hold/rest poses; busy/occupied rejection; Stable; null Held/Selected; no runtime errors.");
                Finish(0);
                return;
            }
            int index = round % 2;
            var bottle = bottles[index];
            if (step % 2 == 0)
            {
                Require(coordinator.TryAcquireBottle(bottle), "acquire");
                Require(coordinator.PrimaryHeld == bottle.gameObject && coordinator.Selected == bottle.gameObject, "held reference");
                Require(!coordinator.SecondaryHeld && coordinator.OwnsHeldInput, "primary only and legacy input suppressed");
                Require(!coordinator.TryAcquireBottle(bottles[1 - index]), "occupied rejects another bottle");
                Require(Vector3.Distance(bottle.transform.position, anchor.position) < .0001f && Quaternion.Angle(bottle.transform.rotation, anchor.rotation) < .01f, "hold pose");
            }
            else
            {
                Require(Vector3.Distance(bottle.transform.position, anchor.position) < .0001f, "hold persists across frames");
                coordinator.SetActionState(ActionState.Flair);
                Require(!coordinator.TryReturnBottle() && coordinator.PrimaryHeld == bottle.gameObject, "busy return rejected");
                coordinator.SetActionState(ActionState.Stable);
                Require(coordinator.TryReturnBottle(), "return");
                Require(!coordinator.PrimaryHeld && !coordinator.Selected && coordinator.CurrentActionState == ActionState.Stable, "returned state");
                Require(Vector3.Distance(bottle.transform.position, rests[index].position) < .0001f && Quaternion.Angle(bottle.transform.rotation, rests[index].rotation) < .01f, "rest pose");
            }
            step++;
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Finish(1);
        }
    }

    static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        EditorApplication.Exit(code);
    }
}
#endif
