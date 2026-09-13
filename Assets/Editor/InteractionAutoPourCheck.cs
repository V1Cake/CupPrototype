#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.Game;
using CupPrototype.Interaction;
using CupPrototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InteractionAutoPourCheck
{
    const string Key = "T15.Running";
    static IEnumerator checks;
    static double deadline;
    static bool error;
    static IngredientData ingredient;
    static IngredientData secondIngredient;
    static InteractionAutoPourCheck() { if (SessionState.GetBool(Key, false)) Hook(); }
    public static void RunBatch()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "saved Edit Mode required");
        checks = null; error = false; SessionState.SetBool(Key, true); Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); EditorApplication.isPlaying = true;
    }
    static void Hook() { deadline = EditorApplication.timeSinceStartup + 180; EditorApplication.update -= Tick; EditorApplication.update += Tick; }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "timeout");
            if (!Application.isPlaying) return;
            Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate(); if (Time.frameCount < 3) return;
            if (checks == null) { Application.logMessageReceived += Log; checks = RunChecks(); }
            Require(!error, "runtime Error / Exception"); if (!checks.MoveNext()) Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static IEnumerator RunChecks()
    {
        var c = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var s = UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        var r = c.GetComponent<CurrentDrinkRecord>(); var source = s.GetComponent<DrinkContainer>();
        var so = new SerializedObject(c);
        var cup = (DrinkContainer)so.FindProperty("rackCups").GetArrayElementAtIndex(0).objectReferenceValue;
        var other = (DrinkContainer)so.FindProperty("rackCups").GetArrayElementAtIndex(1).objectReferenceValue;
        var j = (DrinkContainer)so.FindProperty("jigger").objectReferenceValue;
        var serveIce = ((Transform)so.FindProperty("serveIceWell").objectReferenceValue).GetComponent<Collider>();
        var processIce = ((Transform)so.FindProperty("iceWell").objectReferenceValue).GetComponent<Collider>();
        var rest = new Pose(s.transform.position, s.transform.rotation);
        ingredient = ScriptableObject.CreateInstance<IngredientData>(); ingredient.sourness = 37;
        secondIngredient = ScriptableObject.CreateInstance<IngredientData>(); secondIngredient.sourness = 37;
        var panel = DemoMessagePanel.Instance;
        Require(panel && panel.messageText && panel.messageDuration >= 1.5f && panel.messageDuration <= 2f, "existing short feedback UI");
        int panelCount = UnityEngine.Object.FindObjectsByType<DemoMessagePanel>().Length;
        for (int invalid = 0; invalid < 4; invalid++)
        {
            Require(c.TryAcquireShaker(s), "invalid fixture acquire");
            if (invalid >= 2) { j.AddIngredient(ingredient, 10, false); Require(j.TransferTo(source, 10), "single ingredient"); }
            if (invalid % 2 == 1) Require(c.TryAddProcessIce(processIce), "ice-only/single plus ice");
            Require(c.TryCloseShaker(), "invalid fixture Close"); while(c.CurrentActionState != ActionState.Stable) yield return null;
            for (int attempt = 0; attempt < 2; attempt++)
                Require(!c.TryBeginShakeGesture(s.GetComponent<FlairableTool>()) && !r.ShakePerformed && s.State == ShakerState.ReadyToShake && c.CurrentActionState == ActionState.Stable && !s.HasShakeIngredients, "insufficient actual contents rejected");
            Require(panel.messageText.text == "Not enough ingredients to shake." && UnityEngine.Object.FindObjectsByType<DemoMessagePanel>().Length == panelCount, "one reusable feedback panel");
            if (invalid == 0)
            {
                float until = Time.time + 2.1f; while(Time.time < until) yield return null;
                Require(panel.messageText.text == "", "feedback expires");
            }
            if (invalid == 3) UnityEngine.Object.FindAnyObjectByType<DemoRoundManager>().StartNextTarget(); else Reset(c);
            Require(panel.messageText.text == "", "R/order clears feedback immediately");
        }
        foreach (int mode in new[] { 0, 1, 2, 3, 4, 5 })
        {
            cup = (DrinkContainer)so.FindProperty("rackCups").GetArrayElementAtIndex(mode % 3).objectReferenceValue;
            other = (DrinkContainer)so.FindProperty("rackCups").GetArrayElementAtIndex((mode + 1) % 3).objectReferenceValue;
            Require(!c.TryAcquireServeShaker(s) && !c.TryStartAutoPour(cup), "Rest rejects Pour");
            Require(c.TryAcquireShaker(s), "acquire preparation");
            float amount = mode == 0 ? cup.MaxVolume * .4f : cup.MaxVolume + 30;
            j.AddIngredient(secondIngredient, 1, false); Require(j.TransferTo(source, 1), "second actual ingredient");
            float remaining = amount - 1;
            while (remaining > .001f)
            {
                float portion = Mathf.Min(remaining, j.MaxVolume);
                j.AddIngredient(ingredient, portion, false); Require(j.TransferTo(source, portion), "fixture transfer"); remaining -= portion;
            }
            Require(c.TryAddProcessIce(processIce), "Process Ice"); r.RecordTaste("fixture");
            Require(!c.TryAcquireServeShaker(s), "Preparing rejects");
            Require(c.TryCloseShaker(), "Close"); while (c.CurrentActionState != ActionState.Stable) yield return null;
            Require(!c.TryAcquireServeShaker(s), "Ready rejects");
            Require(c.TryBeginShakeGesture(s.GetComponent<FlairableTool>()) && c.CompleteShakeGesture(new GestureMatchResult { isMatched = true, templateId = "ShakerRoll_01", gestureType = FlairGestureType.Circle }), "Shake");
            while (c.CurrentActionState != ActionState.Stable) yield return null;
            Require(c.PrimaryHeld == s.gameObject && !c.SecondaryHeld && s.State == ShakerState.ShakeComplete && r.ShakePerformed, "successful Shake automatically Serve Held without Cup");
            Require(!c.TryOpenShaker(), "Open requires leaving Serve Held first");
            Require(!c.TryAcquireServeShaker(s), "no Serve Cup rejects");
            Require(c.TryAcquireCup(cup) && c.TryAddServeIce(serveIce), "Cup + optional ice");
            if (mode != 0)
            {
                Require(c.TryReturnHeld() && !c.PrimaryHeld && s.State == ShakerState.ShakeComplete && r.ShakePerformed, "RMB returns without losing Shake");
                Require(c.TryOpenShaker(), "reopen after return"); while (c.CurrentActionState != ActionState.Stable) yield return null;
                Require(!c.TryAcquireServeShaker(s), "reopened Shaker cannot Pour even with retained Shake fact");
                Require(c.TryCloseShaker(), "unchanged Close restores complete"); while (c.CurrentActionState != ActionState.Stable) yield return null;
                Require(c.TryAcquireServeShaker(s), "reacquire from Prep");
            }
            Require(!c.TryStartAutoPour(other) && c.TryStartAutoPour(cup) && !c.TryStartAutoPour(cup), "direct or reacquired Held pours current Cup once");
            Require(c.CurrentActionState == ActionState.AutoPour && !c.TryReturnHeld() && !c.TryOpenShaker() && !c.TryAcquireJigger(j) && !c.TryAcquireCup(other) && !c.TryAddProcessIce(processIce) && !c.TryAddServeIce(serveIce), "Busy conflict rejection");
            while (cup.CurrentVolume <= 0 && c.CurrentActionState == ActionState.AutoPour) yield return null;
            Require(s.State == ShakerState.ShakeComplete, "visual opening never Preparing");
            CupLiquidVisualCheck.Check(cup);
            if (mode >= 3)
            {
                if (mode == 3) Reset(c);
                else if (mode == 4) UnityEngine.Object.FindAnyObjectByType<DemoRoundManager>().StartNextTarget();
                else { s.enabled = false; s.enabled = true; Reset(c); }
                for (int i = 0; i < 10; i++) yield return null;
                Require(c.CurrentActionState == ActionState.Stable && !c.PrimaryHeld && !c.SecondaryHeld && !r.PourCompleted && source.IsEmpty() && cup.IsEmpty() && s.State == ShakerState.Rest, "cancel clean without delayed writes");
                CupLiquidVisualCheck.Check(cup);
                continue;
            }
            bool sawWaste = false;
            while (c.CurrentActionState != ActionState.Stable)
            {
                if (cup.IsFull() && source.CurrentVolume > 1) sawWaste = true;
                CupLiquidVisualCheck.Check(cup);
                yield return null;
            }
            Require(Mathf.Abs(cup.CurrentVolume - Mathf.Min(amount, cup.MaxVolume)) < .01f && cup.CurrentVolume <= cup.MaxVolume + .001f, "actual capacity-limited volume");
            CupLiquidVisualCheck.Check(cup);
            Require(Mathf.Abs(cup.GetCurrentFlavorProfile().sourness - 37) < .01f, "actual ingredient flavor preserved");
            Require(mode == 0 || sawWaste, "remainder retained until return");
            Require(source.IsEmpty() && s.State == ShakerState.Rest && Vector3.Distance(s.transform.position, rest.position) < .001f && Quaternion.Angle(s.transform.rotation, rest.rotation) < .01f, "empty exact Rest pose");
            Require(r.PourCompleted && r.SelectedGlass == cup && r.ProcessIce && r.ServeIce && r.Tasted && r.ShakePerformed && !c.PrimaryHeld, "facts survive Shaker reset");
            Require(!c.TryAcquireCup(other) && !c.TryAcquireServeShaker(s) && !c.TryStartAutoPour(cup), "finished cup locked / no second pour");
            Reset(c);
        }
        Debug.Log("T15_1_AUTOMATED_PASS + T15_AUTOMATED_PASS: ingredient eligibility; reusable timed feedback; automatic Serve Held; direct and reacquired Pour; partial capacity; Waste; facts; cancellation");
    }
    static void Reset(InteractionCoordinator c) => typeof(DrinkTestManager).GetMethod("ClearCurrentDrink", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c.GetComponent<DrinkTestManager>(), null);
    static void Require(bool ok, string message) { if (!ok) throw new Exception("T15: " + message); }
    static void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = true; }
    static void Finish(int code)
    {
        Application.logMessageReceived -= Log; EditorApplication.update -= Tick; SessionState.SetBool(Key, false); SessionState.SetString("T15.Result", code == 0 ? "PASS" : "FAIL");
        if (ingredient) UnityEngine.Object.DestroyImmediate(ingredient);
        if (secondIngredient) UnityEngine.Object.DestroyImmediate(secondIngredient);
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
#endif
