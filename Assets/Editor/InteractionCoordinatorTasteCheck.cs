#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.Game;
using CupPrototype.Interaction;
using CupPrototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class InteractionCoordinatorTasteCheck
{
    const string Key = "T12.TasteRunning";
    static IEnumerator checks;
    static double deadline;
    static bool runtimeError;
    static InteractionCoordinator c;
    static ShakerPreparation s;
    static CurrentDrinkRecord record;
    static StirStickTaste stick;
    static DrinkContainer drink, jigger;
    static Pose rest;
    static Collider click;
    static IngredientData sour, sweet;
    static TargetDrinkData recipe;
    static InteractionCoordinatorTasteCheck() { if (SessionState.GetBool(Key, false)) Hook(); }
    public static void SetupAndRun()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before setup");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var bowl = UnityEngine.Object.FindObjectsByType<Renderer>().Single(r => r.name == "BarSpoon_Bowl");
        var root = PrefabUtility.GetOutermostPrefabInstanceRoot(bowl.gameObject);
        Require(root, "existing spoon prefab root");
        var handle = root.GetComponentsInChildren<Renderer>().Single(r => r.name == "BarSpoon_HandleCap");
        var taste = root.GetComponent<StirStickTaste>(); if (!taste) taste = root.AddComponent<StirStickTaste>();
        var parts = root.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("BarSpoon_")).ToArray();
        {
            var bounds = new Bounds(root.transform.InverseTransformPoint(bowl.bounds.center), Vector3.zero);
            foreach (var renderer in parts)
                for (int i = 0; i < 8; i++)
                    bounds.Encapsulate(root.transform.InverseTransformPoint(renderer.bounds.center + Vector3.Scale(renderer.bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            var collider = root.GetComponent<BoxCollider>(); if (!collider) collider = root.AddComponent<BoxCollider>(); collider.center = bounds.center; collider.size = Vector3.Max(bounds.size, Vector3.one * .025f);
        }
        var shaker = UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        var tin = shaker.GetComponentsInChildren<Renderer>().Single(r => r.name == "Shaker_LargeTin");
        var dip = shaker.transform.Find("TasteDipPoint");
        if (!dip) { dip = new GameObject("TasteDipPoint").transform; dip.SetParent(shaker.transform); }
        dip.position = new Vector3(tin.bounds.center.x, tin.bounds.max.y - .035f, tin.bounds.center.z);
        var camera = Camera.main;
        var sample = camera.transform.Find("TasteSamplePoint");
        if (!sample) { sample = new GameObject("TasteSamplePoint").transform; sample.SetParent(camera.transform, false); }
        sample.localPosition = new Vector3(.12f, -.12f, .5f);
        var so = new SerializedObject(taste);
        var partRefs = so.FindProperty("parts"); partRefs.arraySize = parts.Length;
        for (int i = 0; i < parts.Length; i++) partRefs.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
        so.FindProperty("bowl").objectReferenceValue = bowl; so.FindProperty("handle").objectReferenceValue = handle;
        so.FindProperty("dipPoint").objectReferenceValue = dip; so.FindProperty("samplePoint").objectReferenceValue = sample;
        so.ApplyModifiedPropertiesWithoutUndo();
        var coordinator = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        so = new SerializedObject(coordinator); so.FindProperty("stirStick").objectReferenceValue = taste; so.ApplyModifiedPropertiesWithoutUndo();
        var ui = coordinator.GetComponent<ShakerContextUI>(); so = new SerializedObject(ui);
        if (!so.FindProperty("tasteFeedbackRoot").objectReferenceValue)
        {
            var ice = (GameObject)new SerializedObject(shaker).FindProperty("processIceFeedback").objectReferenceValue;
            var feedback = UnityEngine.Object.Instantiate(ice, ice.transform.parent); feedback.name = "TasteFeedback";
            var rect = feedback.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .085f);
            var text = feedback.GetComponentInChildren<TMP_Text>(true); text.text = string.Empty;
            foreach (var graphic in feedback.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            feedback.SetActive(false);
            so.FindProperty("tasteFeedbackRoot").objectReferenceValue = feedback; so.FindProperty("tasteFeedback").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(root.scene); EditorSceneManager.SaveScene(root.scene);
        Debug.Log("T12 Spoon: " + root.name + " bowl=" + bowl.bounds.center + " dip=" + dip.position);
        RunBatch();
    }
    public static void RunBatch()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before check");
        checks = null; runtimeError = false; SessionState.SetBool(Key, true); Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); EditorApplication.isPlaying = true;
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
        c = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>(); s = UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        record = c.GetComponent<CurrentDrinkRecord>(); stick = UnityEngine.Object.FindAnyObjectByType<StirStickTaste>();
        drink = s.GetComponent<DrinkContainer>(); jigger = (DrinkContainer)new SerializedObject(c).FindProperty("jigger").objectReferenceValue;
        rest = new Pose(stick.transform.position, stick.transform.rotation); click = stick.GetComponent<Collider>();
        var ss = new SerializedObject(stick); var bowl = (Renderer)ss.FindProperty("bowl").objectReferenceValue;
        var dip = (Transform)ss.FindProperty("dipPoint").objectReferenceValue; var sample = (Transform)ss.FindProperty("samplePoint").objectReferenceValue;
        var tools = stick.GetComponentsInChildren<Renderer>();
        var originalPoses = tools.Select(r => new Pose(r.transform.position, r.transform.rotation)).ToArray();
        Physics.SyncTransforms();
        Require(Physics.Raycast(new Ray(Camera.main.transform.position, click.bounds.center - Camera.main.transform.position), out var hit) && hit.collider.GetComponentInParent<StirStickTaste>() == stick, "existing spoon reachable by click");
        Require(!c.TryTaste(click), "Rest rejects"); Require(c.TryAcquireShaker(s) && !c.TryTaste(click), "empty Preparing rejects");
        sour = ScriptableObject.CreateInstance<IngredientData>(); sour.ingredientName = "T12 Sour"; sour.sourness = 100;
        sweet = ScriptableObject.CreateInstance<IngredientData>(); sweet.ingredientName = "T12 Sweet"; sweet.sweetness = 200;
        jigger.AddIngredient(sour, 25, false); Require(jigger.TransferTo(drink, 25), "liquid fixture");
        Require(!c.TryTaste(jigger.GetComponentInChildren<Collider>()), "wrong object rejects");
        Require(TasteFeedbackSystem.GenerateFeedback(new FlavorProfile { sourness = 9, sweetness = 4 }, new FlavorProfile { sweetness = 5 }, 2) == "Too sour.", "largest valid deviation only");
        Require(TasteFeedbackSystem.GenerateFeedback(new FlavorProfile { sweetness = 1 }, new FlavorProfile { sweetness = 5 }, 2) == "Not sweet enough.", "negative deviation");
        Require(TasteFeedbackSystem.GenerateFeedback(new FlavorProfile { sourness = 2 }, new FlavorProfile(), 2) == "Overall close to expected.", "existing tolerance boundary");
        recipe = UnityEngine.Object.Instantiate(record.ActiveOrder.Recipe);
        recipe.requiredIngredients = new System.Collections.Generic.List<IngredientData> { sour, sweet };
        record.BeginOrder(new OrderContext(recipe));
        Require(!record.HasShaken && !record.ShakePerformed, "Taste fixture has never shaken");
        string previousFeedback = null;
        for (int round = 0; round < 3; round++)
        {
            string before = JsonUtility.ToJson(drink); bool shaken = record.ShakePerformed, ice = record.ProcessIce;
            c.SetActionState(ActionState.AutoPour); Require(!c.TryTaste(click), "Busy rejects"); c.SetActionState(ActionState.Stable);
            Require(c.TryTaste(click) && c.CurrentActionState == ActionState.Tasting && c.OwnsHeldInput, "Taste starts / lock");
            Require(!c.TryTaste(click) && !c.TryCloseShaker() && !c.TryReturnHeld(), "conflicting inputs reject");
            UnityEngine.Object.FindAnyObjectByType<RecipeTerminalPrototype>().OpenBrowser();
            Require(!UnityEngine.Object.FindAnyObjectByType<RecipeTerminalPrototype>().IsBrowserOpen, "Tab locked");
            bool dipped = false, sampled = false;
            while (c.CurrentActionState == ActionState.Tasting)
            {
                dipped |= Vector3.Distance(bowl.bounds.center, dip.position) < .04f;
                sampled |= Vector3.Distance(bowl.bounds.center, sample.position) < .04f;
                yield return null;
            }
            for (int i = 0; i < tools.Length; i++) Require(Vector3.Distance(tools[i].transform.position, originalPoses[i].position) < .000001f && Quaternion.Angle(tools[i].transform.rotation, originalPoses[i].rotation) < .001f, "all spoon parts return / other tools unchanged");
            Require(dipped && sampled && AtRest(), "dip / sample / exact return");
            Require(record.Tasted && record.ShakePerformed == shaken && record.ProcessIce == ice && before == JsonUtility.ToJson(drink) && s.State == ShakerState.Preparing, "Taste preserves all gameplay content");
            Require(record.TasteFeedback == TasteFeedbackSystem.GenerateFeedback(drink, record.ActiveOrder.Recipe), "actual Shaker and current order");
            Require(!string.IsNullOrEmpty(record.TasteFeedback) && !record.TasteFeedback.Any(char.IsDigit), "qualitative only");
            CheckFeedback(true);
            Require(round < 2 ? record.TasteFeedback == TasteFeedbackSystem.MissingIngredientFeedback : record.TasteFeedback != TasteFeedbackSystem.MissingIngredientFeedback, "missing ingredient has priority until recipe complete");
            int version = record.PreparationVersion;
            Require(record.LastTastedVersion == version && !c.TryTaste(click), "same version rejects repeat Taste");
            if (round == 0)
            {
                previousFeedback = record.TasteFeedback;
                var well = (Transform)new SerializedObject(c).FindProperty("iceWell").objectReferenceValue;
                Require(c.TryAddProcessIce(well.GetComponent<Collider>()) && record.PreparationVersion == version + 1, "first Ice increments version after Taste");
                Require(c.TryCloseShaker(), "Close after Taste"); while (c.CurrentActionState != ActionState.Stable) yield return null;
                Require(!c.TryTaste(click), "ReadyToShake rejects Taste");
                Shake(); while (c.CurrentActionState != ActionState.Stable) yield return null;
                Require(!c.TryTaste(click), "ShakeComplete rejects Taste");
                Require(c.TryOpenShaker(), "Tasted does not block Open"); while (c.CurrentActionState != ActionState.Stable) yield return null;
            }
            else if (round == 1)
            {
                Require(record.TasteFeedback == previousFeedback && record.ShakePerformed, "repeat fresh Taste preserves Shake");
                Require(c.TryAcquireJigger(jigger), "Held Jigger after Taste");
                Require(!c.TryStartJiggerTransfer(s) && !s.TryAddProcessIce() && record.PreparationVersion == version && !c.TryTaste(click), "empty Transfer and repeated Ice keep tasted version");
                Require(c.TryCloseShaker(), "unchanged Close"); while (c.CurrentActionState != ActionState.Stable) yield return null;
                Require(c.TryOpenShaker(), "unchanged Open"); while (c.CurrentActionState != ActionState.Stable) yield return null;
                Require(record.PreparationVersion == version && !c.TryTaste(click), "Open Close preserve tasted version");
                jigger.AddIngredient(sweet, 25, false);
                Require(c.TryStartJiggerTransfer(s), "Transfer after Taste"); while (c.CurrentActionState != ActionState.Stable) yield return null;
                Require(record.PreparationVersion == version + 1 && !record.ShakePerformed && c.TryReturnHeld(), "real change invalidates Shake");
            }
            else Require(record.TasteFeedback != previousFeedback, "modified drink produces new feedback");
        }
        Require(c.TryCloseShaker(), "changed drink Close"); while (c.CurrentActionState != ActionState.Stable) yield return null;
        Require(s.State == ShakerState.ReadyToShake, "changed drink must re-Shake");
        Shake(); while (c.CurrentActionState != ActionState.Stable) yield return null;
        Require(record.ShakePerformed, "reshaken");
        for (int cancel = 0; cancel < 2; cancel++)
        {
            Reset(); CheckFeedback(false); Require(!record.Tasted && !record.HasShaken && record.PreparationVersion == 0 && record.LastTastedVersion == -1 && c.TryAcquireShaker(s), "R clears Taste");
            jigger.AddIngredient(sour, 20, false); Require(jigger.TransferTo(drink, 20), "cancellation liquid");

            Require(c.TryTaste(click), "cancellation setup");
            if (cancel == 0) Reset(); else UnityEngine.Object.FindAnyObjectByType<DemoRoundManager>().StartNextTarget();
            float start = Time.time; while (Time.time - start < 2.4f) yield return null;
            Require(!record.Tasted && !record.HasShaken && record.PreparationVersion == 0 && record.LastTastedVersion == -1 && string.IsNullOrEmpty(record.TasteFeedback) && AtRest() && c.CurrentActionState == ActionState.Stable, "R / new order cancels old Taste and feedback"); CheckFeedback(false);
        }
        Require(c.TryAcquireShaker(s), "new order Preparing"); jigger.AddIngredient(sweet, 20, false); Require(jigger.TransferTo(drink, 20), "new order liquid");

        Require(c.TryTaste(click), "Taste new order"); while (c.CurrentActionState == ActionState.Tasting) yield return null;
        Require(record.TasteFeedback == TasteFeedbackSystem.GenerateFeedback(drink, record.ActiveOrder.Recipe), "new order expected flavor");
        UnityEngine.Object.FindAnyObjectByType<DemoRoundManager>().StartNextTarget(); CheckFeedback(false); Require(!record.Tasted, "switch clears completed Taste");
        Debug.Log("T12_AUTOMATED_PASS: existing spoon click/dip/sample/return; Preparing/nonempty/order gates; busy/Closed rejection; current actual flavor and order; qualitative tolerance/max deviation; repeat and post-Taste modifications; Shake preserved unless contents change; R/order cancel and feedback cleanup");
    }
    static bool AtRest() => Vector3.Distance(stick.transform.position, rest.position) < .000001f && Quaternion.Angle(stick.transform.rotation, rest.rotation) < .001f;
    static void CheckFeedback(bool visible)
    {
        var ui = c.GetComponent<ShakerContextUI>(); ui.Refresh(); var so = new SerializedObject(ui);
        Require(((GameObject)so.FindProperty("tasteFeedbackRoot").objectReferenceValue).activeSelf == visible && ((TMP_Text)so.FindProperty("tasteFeedback").objectReferenceValue).text == record.TasteFeedback, "data-driven feedback UI");
    }
    static void Shake() { Require(c.TryBeginShakeGesture(s.GetComponent<FlairableTool>()) && c.CompleteShakeGesture(new GestureMatchResult { isMatched = true, templateId = "ShakerRoll_01", gestureType = FlairGestureType.Circle }), "Shake setup"); }
    static void Reset() => typeof(DrinkTestManager).GetMethod("ClearCurrentDrink", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetComponent<DrinkTestManager>(), null);
    static void Require(bool value, string message) { if (!value) throw new Exception("T12: " + message); }
    static void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = true; }
    static void Finish(int code)
    {
        Application.logMessageReceived -= Log; EditorApplication.update -= Tick; SessionState.SetBool(Key, false); SessionState.SetString("T12.TasteCheck", code == 0 ? "PASS" : "FAIL");
        if (recipe) UnityEngine.Object.DestroyImmediate(recipe);
        if (sour) UnityEngine.Object.DestroyImmediate(sour); if (sweet) UnityEngine.Object.DestroyImmediate(sweet);
        if (Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying = false;
    }
}
#endif
