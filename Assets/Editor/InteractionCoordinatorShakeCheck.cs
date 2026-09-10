#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.Interaction;
using CupPrototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InteractionCoordinatorShakeCheck
{
    const string Key = "T09.ShakeBatch";
    static IEnumerator checks;
    static double deadline;
    static InteractionCoordinator coordinator;
    static ShakerPreparation shaker;
    static FlairGestureController gestures;
    static FlairableTool tool;
    static CurrentDrinkRecord record;
    static DrinkContainer liquid;
    static Transform lid, prep;
    static Vector3 closedLid;
    static bool runtimeError;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static InteractionCoordinatorShakeCheck() { if (SessionState.GetBool(Key, false)) Hook(); }

    public static void SetupAndRun()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before setup");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var s = UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        var t = s.GetComponent<FlairableTool>();
        var large = s.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "Shaker_LargeTin");
        var small = s.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "Shaker_SmallTin");
        var visual = large.transform.parent;
        while (!small.transform.IsChildOf(visual)) visual = visual.parent;
        Require(visual != s.transform, "shared visual root");
        t.visualRoot = visual;
        var action = t.actions.Single(a => a.requiredTemplateId == "ShakerRoll_01");
        action.testAnimationType = FlairTestAnimationType.Shake;
        action.duration = .9f; action.shakeDistance = .08f; action.shakeCount = 4;
        var camera = Camera.main;
        var anchor = camera.transform.Find("ShakerShakeAnchor");
        if (!anchor) { anchor = new GameObject("ShakerShakeAnchor").transform; anchor.SetParent(camera.transform, false); }
        var bounds = large.bounds; bounds.Encapsulate(small.bounds);
        anchor.SetPositionAndRotation(camera.transform.TransformPoint(new Vector3(0, -.03f, .7f)) - (bounds.center - s.transform.position), s.transform.rotation);
        var serialized = new SerializedObject(s);
        serialized.FindProperty("shakePosition").objectReferenceValue = anchor;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
        EditorSceneManager.SaveScene(s.gameObject.scene);
        RunBatch();
    }

    public static void RunBatch()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before check");
        SessionState.SetBool(Key, true); Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
    }
    static void Hook()
    {
        deadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "timeout");
            if (!Application.isPlaying) return;
            Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate();
            if (Time.frameCount < 3) return;
            if (checks == null) { Application.logMessageReceived += Log; checks = RunChecks(); }
            Require(!runtimeError, "runtime Error / Exception");
            if (!checks.MoveNext()) Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static IEnumerator RunChecks()
    {
        coordinator = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        shaker = UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        gestures = UnityEngine.Object.FindAnyObjectByType<FlairGestureController>();
        tool = shaker.GetComponent<FlairableTool>();
        record = UnityEngine.Object.FindAnyObjectByType<CurrentDrinkRecord>();
        liquid = shaker.GetComponent<DrinkContainer>();
        var so = new SerializedObject(shaker);
        lid = (Transform)so.FindProperty("lid").objectReferenceValue;
        prep = (Transform)so.FindProperty("prepPosition").objectReferenceValue;
        closedLid = lid.localPosition;
        Require(!shaker.GetComponent<ShakerController>().enabled, "old movement Shake disabled");
        Require(!gestures.TryStartRecording(ShakerRay(), false), "Rest rejects Shake");
        Require(!coordinator.CompleteShakeGesture(GestureMatchResult.None), "no start rejects result");

        for (int round = 0; round < 4; round++)
        {
            Reset();
            Require(coordinator.TryAcquireShaker(shaker), "Acquire");
            Require(!gestures.TryStartRecording(ShakerRay(), false), "Preparing rejects Shake");
            if (round > 0)
            {
                var well = (Transform)new SerializedObject(coordinator).FindProperty("iceWell").objectReferenceValue;
                Require(coordinator.TryAddProcessIce(well.GetComponent<Collider>()), "Ice setup");
            }
            var bottle = GameObject.Find("Bottle_Vodka").GetComponent<PourableIngredient>();
            var jigger = (DrinkContainer)new SerializedObject(coordinator).FindProperty("jigger").objectReferenceValue;
            jigger.AddIngredient(bottle.ingredientData, 25, false);
            Require(jigger.TransferTo(liquid, 25), "liquid setup");
            Require(coordinator.TryCloseShaker(), "Close");
            Require(!gestures.TryStartRecording(ShakerRay(), false), "Closing rejects Gesture");
            while (coordinator.CurrentActionState == ActionState.Closing) yield return null;
            var before = JsonUtility.ToJson(liquid);
            bool ice = record.ProcessIce;
            Require(!gestures.TryStartRecording(new Ray(Camera.main.transform.position, Vector3.up), false), "blank start rejected");
            var bottleRay = new Ray(Camera.main.transform.position, bottle.GetComponentInChildren<Collider>().bounds.center - Camera.main.transform.position);
            Require(!gestures.TryStartRecording(bottleRay, false), "wrong object rejected");
            Require(coordinator.TryAcquireBottle(bottle) && coordinator.TryAcquireJigger(jigger), "Held setup");
            Require(!gestures.TryStartRecording(ShakerRay(), false), "occupied hands rejected");
            Require(coordinator.TryReturnHeld() && coordinator.TryReturnHeld(), "release hands");
            coordinator.SetActionState(ActionState.AutoPour);
            Require(!gestures.TryStartRecording(ShakerRay(), false), "busy rejected");
            coordinator.SetActionState(ActionState.Stable);
            Require(gestures.TryStartRecording(ShakerRay(), false), "failure sampling starts on Shaker");
            FinishGesture(false);
            Require(!record.ShakePerformed && shaker.State == ShakerState.ReadyToShake && !tool.IsPlayingFlair && coordinator.CurrentActionState == ActionState.Stable, "failure stays Ready / retryable");
            Require(JsonUtility.ToJson(liquid) == before && record.ProcessIce == ice, "failure preserves data");
            Require(gestures.TryStartRecording(ShakerRay(), false), "retry immediately");
            Require(coordinator.CurrentActionState == ActionState.Flair && coordinator.OwnsHeldInput && !coordinator.TryReturnHeld(), "sampling lock");
            if (round == 3)
            {
                Reset();
                Require(!FlairGestureController.IsFlairInputActive && coordinator.CurrentActionState == ActionState.Stable, "Reset cancels sampling");
                Require(!coordinator.CompleteShakeGesture(new GestureMatchResult { isMatched = true, templateId = "ShakerRoll_01" }), "stale result rejected");
                break;
            }
            FinishGesture(true);
            Require(coordinator.CurrentActionState == ActionState.Shake && tool.IsPlayingFlair && coordinator.PrimaryHeld == shaker.gameObject && !record.ShakePerformed, "success starts animation, not completed fact");
            Require(!gestures.TryStartRecording(ShakerRay(), false) && !coordinator.TryCloseShaker() && !coordinator.TryReturnHeld(), "animation rejects conflicting input");
            UnityEngine.Object.FindAnyObjectByType<RecipeTerminalPrototype>().OpenBrowser();
            Require(!UnityEngine.Object.FindAnyObjectByType<RecipeTerminalPrototype>().IsBrowserOpen, "Tab locked");
            if (round == 2)
            {
                Reset();
                yield return null;
                Require(!tool.IsPlayingFlair && !record.ShakePerformed && shaker.State == ShakerState.Rest && coordinator.CurrentActionState == ActionState.Stable, "R cancels animation without completion");
                continue;
            }
            var initialVisual = tool.visualRoot.localPosition;
            bool moved = false;
            float started = Time.time;
            while (coordinator.CurrentActionState == ActionState.Shake)
            {
                moved |= Vector3.Distance(initialVisual, tool.visualRoot.localPosition) > .005f;
                Require(!record.ShakePerformed, "fact waits for animation callback");
                foreach (var renderer in tool.visualRoot.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled) continue;
                    var viewport = Camera.main.WorldToViewportPoint(renderer.bounds.center);
                    Require(viewport.z > Camera.main.nearClipPlane && viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1, "visible Shake pose");
                }
                yield return null;
            }
            Require(moved && Time.time - started >= tool.actions[0].duration - .06f, "configured duration / visible animation");
            Require(record.ShakePerformed && shaker.State == ShakerState.ShakeComplete && coordinator.CurrentActionState == ActionState.Stable, "completion callback records Shake");
            Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld && !FlairGestureController.IsFlairPlaying, "Held and animation lock released");
            Require(Vector3.Distance(shaker.transform.position, prep.position) < .000001f && Quaternion.Angle(shaker.transform.rotation, prep.rotation) < .001f && lid.localPosition == closedLid, "Closed at Prep");
            Require(JsonUtility.ToJson(liquid) == before && record.ProcessIce == ice, "Shake preserves exact liquid / Process Ice");
            Require(!gestures.TryStartRecording(ShakerRay(), false), "ShakeComplete rejects repeated Shake");
            Reset();
            Require(!record.ShakePerformed && !record.ProcessIce && shaker.State == ShakerState.Rest, "R clears facts / Rest");
        }
        Debug.Log("T09_AUTOMATED_PASS: start-object/state/empty-hand gates; existing template recognizer; failure/retry; configured animation/callback; Closed/Prep/Held; liquid/Ice unchanged; R complete/animation/sampling reset; old Shake disabled");
    }
    static Ray ShakerRay()
    {
        Physics.SyncTransforms();
        return new Ray(Camera.main.transform.position, shaker.GetComponentInChildren<Collider>().bounds.center - Camera.main.transform.position);
    }
    static void FinishGesture(bool success)
    {
        var points = (List<Vector2>)typeof(FlairGestureController).GetField("recordedPoints", Private).GetValue(gestures);
        points.Clear();
        if (success)
        {
            var template = gestures.templateLibrary.GetTemplates().Single(t => t.templateId == "ShakerRoll_01");
            points.AddRange(template.normalizedPoints.Select(p => p * 300 + new Vector2(500, 400)));
        }
        else points.Add(Vector2.zero);
        typeof(FlairGestureController).GetMethod("FinishRecording", Private).Invoke(gestures, null);
    }
    static void Reset() => typeof(DrinkTestManager).GetMethod("ClearCurrentDrink", Private).Invoke(coordinator.GetComponent<DrinkTestManager>(), null);
    static void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = true; }
    static void Require(bool value, string message) { if (!value) throw new Exception("T09: " + message); }
    static void Finish(int code)
    {
        Application.logMessageReceived -= Log;
        SessionState.SetBool(Key, false); SessionState.SetString("T09.ShakeCheck", code == 0 ? "PASS" : "FAIL");
        EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
#endif
