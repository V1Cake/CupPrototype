#if UNITY_EDITOR
using System;
using System.Reflection;
using CupPrototype.DrinkSystem;
using CupPrototype.Game;
using CupPrototype.Interaction;
using CupPrototype.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MinimalOrderContextCheck
{
    const string Key = "T11.OrderCheckRunning";
    static double deadline;
    static MinimalOrderContextCheck() { if (SessionState.GetBool(Key, false)) Hook(); }
    public static void RunBatch()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before validation");
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
            RunChecks(); Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void RunChecks()
    {
        var rounds = UnityEngine.Object.FindObjectsByType<DemoRoundManager>();
        var records = UnityEngine.Object.FindObjectsByType<CurrentDrinkRecord>();
        Require(rounds.Length == 1 && records.Length == 1, "one order owner / one record");
        var manager = rounds[0]; var record = records[0];
        var legacy = UnityEngine.Object.FindAnyObjectByType<DrinkTestManager>();
        var bridge = UnityEngine.Object.FindAnyObjectByType<GameplayUIBridge>();
        Require(manager.AvailableTargets.Count >= 2 && manager.ActiveOrder != null, "startup order");
        CheckOrder(manager, record, legacy, bridge);
        for (int i = 0; i < 2; i++)
        {
            var previous = manager.ActiveOrder;
            record.TryRecordProcessIce(); record.RecordShake(); record.RecordTaste();
            previous.RecordRemake(); previous.RecordRemake();
            Require(previous.AttemptCount == 3 && previous.RemakeOccurred && record.Tasted, "stats-only Remake interface");
            bool observedCleanRecord = false;
            Action changed = () =>
            {
                if (!ReferenceEquals(record.ActiveOrder, previous))
                {
                    Require(ReferenceEquals(record.ActiveOrder, manager.ActiveOrder) && !record.ProcessIce && !record.ShakePerformed && !record.Tasted, "new order event is clean");
                    observedCleanRecord = true;
                }
            };
            record.Changed += changed;
            try { manager.StartNextTarget(); }
            finally { record.Changed -= changed; }
            Require(observedCleanRecord && !ReferenceEquals(previous, manager.ActiveOrder), "new order context / record reset");
            Require(previous.AttemptCount == 3, "previous order stats unchanged");
            CheckOrder(manager, record, legacy, bridge);
            Require(manager.ActiveOrder.AttemptCount == 1 && !manager.ActiveOrder.RemakeOccurred, "new order counters");
            Require(!record.ProcessIce && !record.ShakePerformed && !record.Tasted, "no leaked preparation facts");
            Require(manager.State == DemoRoundState.Mixing, "existing target switch lifecycle");
        }
        var active = manager.ActiveOrder;
        active.RecordRemake(); record.TryRecordProcessIce(); record.RecordShake(); record.RecordTaste();
        manager.SubmitRound(); Require(manager.State == DemoRoundState.Submitted, "existing Submitted state");
        typeof(DrinkTestManager).GetMethod("ClearCurrentDrink", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(legacy, null);
        Require(ReferenceEquals(active, manager.ActiveOrder) && active.AttemptCount == 2 && active.RemakeOccurred, "R does not create new order or alter attempt stats");
        Require(!record.ProcessIce && !record.ShakePerformed && !record.Tasted && manager.State == DemoRoundState.Mixing, "existing R resets attempt");
        // Even when the existing optional liquid reset is off, new orders must clear preparation facts.
        var settings = new SerializedObject(manager);
        var clear = settings.FindProperty("clearContainersOnTargetSwitch"); bool wasClear = clear.boolValue;
        clear.boolValue = false; settings.ApplyModifiedPropertiesWithoutUndo();
        var coordinator = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var shaker = UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        Require(coordinator.TryAcquireShaker(shaker) && coordinator.TryCloseShaker(), "in-flight action before order switch");
        record.TryRecordProcessIce(); record.RecordShake(); record.RecordTaste();
        try { manager.SetTarget(0); }
        finally { settings.Update(); settings.FindProperty("clearContainersOnTargetSwitch").boolValue = wasClear; settings.ApplyModifiedPropertiesWithoutUndo(); }
        Require(!record.ProcessIce && !record.ShakePerformed && !record.Tasted, "record reset independent of liquid reset option");
        Require(shaker.State == ShakerState.Rest && coordinator.CurrentActionState == ActionState.Stable, "old action cancelled even when liquid reset is off");
        CheckOrder(manager, record, legacy, bridge);
        var explicitRequirements = new OrderContext(manager.CurrentTarget, "TestGlass", false, true);
        Require(explicitRequirements.RecommendedGlass == "TestGlass" && explicitRequirements.ProcessIceRequired == false && explicitRequirements.ServeIceRequired == true, "nullable requirements preserve explicit false / true");
        Debug.Log("T11_AUTOMATED_PASS: unique ActiveOrder; startup + two order switches; shared Recipe / ExpectedFlavor / UI / scoring target; no fact leakage; null requirements; AttemptCount / RemakeOccurred; R and existing round lifecycle; reset independent of liquid option");
    }
    static void CheckOrder(DemoRoundManager manager, CurrentDrinkRecord record, DrinkTestManager legacy, GameplayUIBridge bridge)
    {
        var order = manager.ActiveOrder;
        Require(order != null && ReferenceEquals(order, record.ActiveOrder), "same ActiveOrder");
        Require(order.Recipe == manager.CurrentTarget && order.Recipe == legacy.CurrentTarget && order.Recipe == bridge.CurrentOrder && order.Recipe == manager.orderDisplay.targetDrink, "same target in all consumers");
        Require(order.ExpectedFlavor.Equals(order.Recipe.targetFlavor) && order.ExpectedFlavor.Equals(bridge.TargetTaste), "reuse existing flavor target");
        Require(order.RecommendedGlass == null && !order.ProcessIceRequired.HasValue && !order.ServeIceRequired.HasValue, "missing requirements remain unknown");
    }
    static void Require(bool value, string message) { if (!value) throw new Exception("T11: " + message); }
    static void Finish(int code)
    {
        SessionState.SetBool(Key, false); SessionState.SetString("T11.OrderCheck", code == 0 ? "PASS" : "FAIL");
        EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
#endif
