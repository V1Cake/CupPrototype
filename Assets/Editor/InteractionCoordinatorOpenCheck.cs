#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.Interaction;
using CupPrototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class InteractionCoordinatorOpenCheck
{
    const string Key = "T10.OpenBatch";
    static IEnumerator checks;
    static double deadline;
    static bool runtimeError;
    static InteractionCoordinator coordinator;
    static ShakerPreparation shaker;
    static ShakerContextUI ui;
    static CurrentDrinkRecord record;
    static DrinkContainer liquid, jigger;
    static Transform lid, prep;
    static Vector3 closedLid, restPosition;
    static Button open;
    static InteractionCoordinatorOpenCheck() { if (SessionState.GetBool(Key, false)) Hook(); }

    public static void SetupAndRun()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before setup");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var context = UnityEngine.Object.FindAnyObjectByType<ShakerContextUI>();
        var so = new SerializedObject(context);
        if (!so.FindProperty("openButton").objectReferenceValue)
        {
            var close = (Button)so.FindProperty("closeButton").objectReferenceValue;
            var button = UnityEngine.Object.Instantiate(close, close.transform.parent);
            button.name = "OpenShakerButton";
            button.GetComponentInChildren<TMP_Text>(true).text = "[OPEN SHAKER]";
            button.gameObject.SetActive(false);
            so.FindProperty("openButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(context.gameObject.scene);
            EditorSceneManager.SaveScene(context.gameObject.scene);
        }
        RunBatch();
    }
    public static void RunBatch()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before validation");
        checks = null; runtimeError = false;
        SessionState.SetBool(Key, true); Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
    }
    static void Hook() { deadline = EditorApplication.timeSinceStartup + 120; EditorApplication.update -= Tick; EditorApplication.update += Tick; }
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
        ui = UnityEngine.Object.FindAnyObjectByType<ShakerContextUI>();
        record = UnityEngine.Object.FindAnyObjectByType<CurrentDrinkRecord>();
        liquid = shaker.GetComponent<DrinkContainer>();
        jigger = (DrinkContainer)new SerializedObject(coordinator).FindProperty("jigger").objectReferenceValue;
        var so = new SerializedObject(shaker);
        lid = (Transform)so.FindProperty("lid").objectReferenceValue;
        prep = (Transform)so.FindProperty("prepPosition").objectReferenceValue;
        closedLid = lid.localPosition; restPosition = shaker.transform.position;
        open = (Button)new SerializedObject(ui).FindProperty("openButton").objectReferenceValue;
        Require(open && open.GetComponentInChildren<TMP_Text>(true).text == "[OPEN SHAKER]", "Scene UI binding");
        var bottle = GameObject.Find("Bottle_Vodka").GetComponent<PourableIngredient>();
        for (int round = 0; round < 3; round++)
        {
            Reset(); ui.Refresh();
            Require(!ui.IsOpenVisible && !ui.IsCloseVisible && !coordinator.TryOpenShaker(), "Rest hides / rejects");
            Require(coordinator.TryAcquireShaker(shaker), "Acquire");
            var well = (Transform)new SerializedObject(coordinator).FindProperty("iceWell").objectReferenceValue;
            Require(coordinator.TryAddProcessIce(well.GetComponent<Collider>()), "Ice setup");
            jigger.AddIngredient(bottle.ingredientData, 25, false); Require(jigger.TransferTo(liquid, 25), "liquid setup");
            Require(coordinator.TryCloseShaker(), "Close");
            while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
            if (round > 0)
            {
                StartShake(); while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
                Require(record.ShakePerformed && shaker.State == ShakerState.ShakeComplete, "Shake setup");
            }
            var previous = shaker.State; var before = JsonUtility.ToJson(liquid);
            bool wasShaken = record.ShakePerformed;
            ui.Refresh(); Require(ui.IsOpenVisible && !ui.IsCloseVisible, "Open shown in eligible state");
            foreach (var busy in new[] { ActionState.AutoPour, ActionState.AutoTransfer, ActionState.Flair, ActionState.Shake, ActionState.Closing, ActionState.Opening })
            {
                coordinator.SetActionState(busy); ui.Refresh();
                Require(!ui.IsOpenVisible && !ui.IsCloseVisible && !coordinator.TryOpenShaker() && record.ShakePerformed == wasShaken && shaker.State == previous && JsonUtility.ToJson(liquid) == before, busy + " rejects without mutation");
                coordinator.SetActionState(ActionState.Stable);
            }
            if (round < 2)
            {
                // Taste is a fixture fact here; no Taste gameplay is executed.
                record.RecordTaste(); ui.Refresh();
                Require(ui.IsOpenVisible && coordinator.CanOpenShaker && shaker.CanOpen, "T12: Tasted no longer blocks Open");
                Require(record.ShakePerformed == wasShaken && record.ProcessIce && JsonUtility.ToJson(liquid) == before && shaker.State == previous, "Tasted preserves data");
            }
            Require(coordinator.TryAcquireBottle(bottle) && coordinator.TryAcquireJigger(jigger), "dual Held setup");
            ui.Refresh(); open.onClick.Invoke();
            Require(coordinator.CurrentActionState == ActionState.Opening && record.ShakePerformed == wasShaken, "Open preserves Shake fact");
            Require(!ui.IsOpenVisible && !ui.IsCloseVisible && !coordinator.TryOpenShaker() && !coordinator.TryCloseShaker() && !coordinator.TryReturnHeld() && !coordinator.TryStartJiggerTransfer(shaker), "Opening lock");
            if (round == 2)
            {
                Reset();
                float resetTime = Time.time;
                while (Time.time - resetTime < .5f) yield return null;
                Require(shaker.State == ShakerState.Rest && coordinator.CurrentActionState == ActionState.Stable && !record.ShakePerformed && !record.ProcessIce && !record.Tasted && !ui.IsOpenVisible && !ui.IsCloseVisible && shaker.transform.position == restPosition, "R cancels Open / clears facts and UI");
                break;
            }
            while (coordinator.CurrentActionState == ActionState.Opening) yield return null;
            ui.Refresh(); Require(shaker.State == ShakerState.Preparing && coordinator.CurrentActionState == ActionState.Stable && ui.IsCloseVisible && !ui.IsOpenVisible, "Open callback / buttons");
            Require(Vector3.Distance(lid.localPosition, closedLid) > .01f && shaker.transform.position == prep.position, "visible open / Prep");
            Require(JsonUtility.ToJson(liquid) == before && record.ProcessIce && record.ShakePerformed == wasShaken, "Open preserves exact liquid / Ice / Shake");
            Require(coordinator.PrimaryHeld == bottle.gameObject && coordinator.SecondaryHeld == jigger.gameObject, "Held preserved");
            int factChanges = 0;
            Action countChange = () => factChanges++;
            record.Changed += countChange;
            try
            {
                for (int click = 0; click < 3; click++)
                    Require(!coordinator.TryAddProcessIce(well.GetComponent<Collider>()), "Reopen cannot add Process Ice twice");
                Require(!shaker.TryAddProcessIce(), "direct Ice entry also rejects repeat");
                Require(factChanges == 0 && record.ProcessIce && shaker.ProcessIce && record.ShakePerformed == wasShaken && JsonUtility.ToJson(liquid) == before, "repeat Ice has no fact event / liquid mutation");
            }
            finally { record.Changed -= countChange; }
            Require(!coordinator.TryStartJiggerTransfer(shaker) && record.ShakePerformed == wasShaken, "empty Transfer preserves Shake");
            if (wasShaken)
            {
                Require(coordinator.TryReturnHeld() && coordinator.TryReturnHeld() && coordinator.TryCloseShaker(), "unchanged Close");
                while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
                Require(shaker.State == ShakerState.ShakeComplete && record.ShakePerformed &&
                    !coordinator.TryBeginShakeGesture(shaker.GetComponent<FlairableTool>()), "unchanged Close restores complete / rejects Shake");
                Require(coordinator.TryOpenShaker(), "second Open");
                while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
                Require(coordinator.TryAcquireBottle(bottle) && coordinator.TryAcquireJigger(jigger), "reacquire tools");
            }
            jigger.AddIngredient(bottle.ingredientData, 10, false);
            Require(coordinator.TryStartJiggerTransfer(shaker), "Reopen accepts Transfer");
            Require(record.ShakePerformed == wasShaken, "starting animation alone preserves Shake");
            while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
            Require(Mathf.Abs(liquid.CurrentVolume - 35) < .001f && jigger.IsEmpty() && !record.ShakePerformed, "actual Transfer clears Shake");
            Require(coordinator.TryReturnHeld() && coordinator.TryReturnHeld() && coordinator.TryCloseShaker(), "release / reclose");
            while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
            Require(!record.ShakePerformed && shaker.State == ShakerState.ReadyToShake, "Close cannot restore previous Shake");
            StartShake(); while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
            Require(record.ShakePerformed && shaker.State == ShakerState.ShakeComplete && record.ProcessIce && Mathf.Abs(liquid.CurrentVolume - 35) < .001f, "must Shake again");
        }
        Reset();
        coordinator.TryReturnHeld(); coordinator.TryReturnHeld();
        Require(coordinator.TryAcquireShaker(shaker) && coordinator.TryCloseShaker(), "no-Ice setup");
        while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
        StartShake(); while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
        Require(coordinator.TryOpenShaker(), "Open for first Ice");
        while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
        Require(record.ShakePerformed && !record.ProcessIce, "Open retains no-Ice Shake");
        var iceWell = (Transform)new SerializedObject(coordinator).FindProperty("iceWell").objectReferenceValue;
        Require(coordinator.TryAddProcessIce(iceWell.GetComponent<Collider>()) && record.ProcessIce && !record.ShakePerformed, "first Ice invalidates Shake");
        Require(coordinator.TryCloseShaker(), "Close after Ice");
        while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
        Require(shaker.State == ShakerState.ReadyToShake, "new Ice requires Shake");
        StartShake(); while (coordinator.CurrentActionState != ActionState.Stable) yield return null;
        Require(record.ShakePerformed && shaker.State == ShakerState.ShakeComplete, "new Ice reshaken");
        Reset();
        Debug.Log("T10_AUTOMATED_PASS: unchanged Open/Close preserves ShakeComplete and rejects Shake; actual liquid/new Ice clears Shake; empty Transfer/repeated Ice preserve Shake; retransfer/reclose/reshake; data/UI/Busy/Tasted/Reset regression");
    }
    static void StartShake()
    {
        Require(coordinator.TryBeginShakeGesture(shaker.GetComponent<FlairableTool>()), "Shake start");
        Require(coordinator.CompleteShakeGesture(new GestureMatchResult { isMatched = true, templateId = "ShakerRoll_01", gestureType = FlairGestureType.Circle }), "Shake action");
    }
    static void Reset() => typeof(DrinkTestManager).GetMethod("ClearCurrentDrink", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(coordinator.GetComponent<DrinkTestManager>(), null);
    static void Require(bool value, string message) { if (!value) throw new Exception("T10: " + message); }
    static void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = true; }
    static void Finish(int code)
    {
        Application.logMessageReceived -= Log; EditorApplication.update -= Tick;
        SessionState.SetBool(Key, false); SessionState.SetString("T10.OpenCheck", code == 0 ? "PASS" : "FAIL");
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
#endif
