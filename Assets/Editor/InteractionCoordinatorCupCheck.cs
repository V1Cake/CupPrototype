#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.Game;
using CupPrototype.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InteractionCoordinatorCupCheck
{
    const string Key = "T13.Running";
    static IEnumerator checks;
    static double deadline;
    static bool error;
    static IngredientData ingredient;
    static InteractionCoordinatorCupCheck() { if (SessionState.GetBool(Key, false)) Hook(); }
    public static void SetupAndRun()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before setup");
        var zone = GameObject.Find("ServiceZone").transform;
        zone.position = new Vector3(.62f, .90f, .97f);
        var cups = zone.GetComponentsInChildren<Transform>().Where(t => t.name.EndsWith("_Prop") && t.name.Contains("Glass")).Select(t =>
        {
            var cup = t.GetComponent<DrinkContainer>(); if (!cup) cup = t.gameObject.AddComponent<DrinkContainer>();
            cup.containerType = DrinkContainer.ContainerType.FinalGlass; cup.containerName = t.name;
            var bounds = VisibleBounds(t.gameObject);
            var collider = t.GetComponent<BoxCollider>(); if (!collider) collider = t.gameObject.AddComponent<BoxCollider>();
            collider.center = t.InverseTransformPoint(bounds.center);
            collider.size = new Vector3(bounds.size.x / t.lossyScale.x, bounds.size.y / t.lossyScale.y, bounds.size.z / t.lossyScale.z);
            var display = t.name switch
            {
                "CoupeGlass_01_Prop" => new Vector2(.47f, .88f),
                "HighballGlass_01_Prop" => new Vector2(.56f, .83f),
                "HighballGlass_02_Prop" => new Vector2(.67f, .89f),
                _ => new Vector2(.74f, .84f)
            };
            t.position += new Vector3(display.x - bounds.center.x, 0, display.y - bounds.center.z);
            return cup;
        }).OrderBy(c => c.name).ToArray();
        Require(cups.Length == 4, "four existing Rack cups");
        var anchor = GameObject.Find("ServePosition"); if (!anchor) anchor = new GameObject("ServePosition");
        anchor.transform.SetPositionAndRotation(new Vector3(-.20f, .925f, .85f), Quaternion.identity);
        var coordinator = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var so = new SerializedObject(coordinator); var list = so.FindProperty("rackCups"); list.arraySize = cups.Length;
        for (int i = 0; i < cups.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = cups[i];
        so.FindProperty("servePosition").objectReferenceValue = anchor.transform; so.ApplyModifiedPropertiesWithoutUndo();
        Physics.SyncTransforms();
        foreach (var cup in cups) CheckClickable(cup.gameObject);
        CheckExisting();
        foreach (var renderer in zone.GetComponentsInChildren<Renderer>()) CheckVisible(renderer.bounds, renderer.name);
        EditorSceneManager.MarkSceneDirty(zone.gameObject.scene); EditorSceneManager.SaveScene(zone.gameObject.scene);
        RunBatch();
    }
    public static void RunBatch()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "save Scene before check");
        checks = null; error = false; SessionState.SetBool(Key, true); Hook();
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
            Require(!error, "runtime Error / Exception");
            if (!checks.MoveNext()) Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static IEnumerator RunChecks()
    {
        var c = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>(); var record = c.GetComponent<CurrentDrinkRecord>();
        var shaker = UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>(); var so = new SerializedObject(c);
        var list = so.FindProperty("rackCups"); var cups = Enumerable.Range(0, list.arraySize).Select(i => (DrinkContainer)list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var rest = cups.Select(cup => new Pose(cup.transform.position, cup.transform.rotation)).ToArray();
        var anchor = (Transform)so.FindProperty("servePosition").objectReferenceValue;
        Require(cups.Length == 4 && !record.SelectedGlass, "initial four Rack cups, no selected glass");
        foreach (var cup in cups) { CheckClickable(cup.gameObject); Require(!cup.GetComponent<FlairableTool>(), "no Rack or served Cup Flair target"); }
        CheckExisting();
        Require(!c.TryAcquireCup(cups[0]), "Rest rejects Cup");
        Require(c.TryAcquireShaker(shaker) && !c.TryAcquireCup(cups[0]), "Preparing rejects Cup");
        Require(c.TryCloseShaker() && !c.TryAcquireCup(cups[0]), "Closing rejects Cup"); while(c.CurrentActionState != ActionState.Stable) yield return null;
        Require(!c.TryAcquireCup(cups[0]), "Ready rejects Cup");
        Require(c.TryBeginShakeGesture(shaker.GetComponent<FlairableTool>()) && c.CompleteShakeGesture(new GestureMatchResult { isMatched = true, templateId = "ShakerRoll_01", gestureType = FlairGestureType.Circle }), "Shake fixture");
        while(c.CurrentActionState != ActionState.Stable) yield return null;
        int version = record.PreparationVersion; bool tasted = record.Tasted;
        for (int i = 0; i < cups.Length; i++)
        {
            c.SetActionState(ActionState.AutoPour); Require(!c.TryAcquireCup(cups[i]), "Busy rejects Cup"); c.SetActionState(ActionState.Stable);
            Require(c.TryAcquireCup(cups[i]) && record.SelectedGlass == cups[i] && c.Selected == cups[i].gameObject && c.CurrentActionState == ActionState.Stable, "select actual Cup / Stable");
            var bounds = VisibleBounds(cups[i].gameObject);
            Require(Vector3.Distance(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), anchor.position) < .0001f, "Cup bottom at ServePosition");
            CheckClickable(cups[i].gameObject); CheckExisting();
            Require(!c.TryAcquireCup(cups[i]), "served Cup cannot be reacquired from Rack");
            if (i > 0) Require(At(cups[i-1].transform, rest[i-1]), "old empty Cup returns exactly");
            Require(record.ShakePerformed && record.Tasted == tasted && record.PreparationVersion == version, "Cup change does not change preparation");
        }
        ingredient = ScriptableObject.CreateInstance<IngredientData>(); ingredient.sweetness = 12;
        cups[3].AddIngredient(ingredient, .001f, false);
        var contents = JsonUtility.ToJson(cups[3]); var selectedPose = new Pose(cups[3].transform.position, cups[3].transform.rotation);
        Require(!c.TryAcquireCup(cups[0]) && record.SelectedGlass == cups[3] && contents == JsonUtility.ToJson(cups[3]) && At(cups[3].transform, selectedPose) && At(cups[0].transform, rest[0]), "any positive volume rejects replacement without mutation");
        typeof(DrinkTestManager).GetMethod("ClearCurrentDrink", BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c.GetComponent<DrinkTestManager>(), null);
        Require(!record.SelectedGlass && At(cups[3].transform, rest[3]) && cups[3].CurrentVolume == 0, "R returns and clears selected Cup");
        // Repeat with a new attempt and exercise the order-switch reset path.
        Require(c.TryAcquireShaker(shaker) && c.TryCloseShaker(), "new attempt Close"); while(c.CurrentActionState != ActionState.Stable) yield return null;
        Require(c.TryBeginShakeGesture(shaker.GetComponent<FlairableTool>()) && c.CompleteShakeGesture(new GestureMatchResult { isMatched=true, templateId="ShakerRoll_01", gestureType=FlairGestureType.Circle }), "new attempt Shake"); while(c.CurrentActionState != ActionState.Stable) yield return null;
        Require(c.TryAcquireCup(cups[1]), "new attempt select");
        UnityEngine.Object.FindAnyObjectByType<DemoRoundManager>().StartNextTarget();
        Require(!record.SelectedGlass && At(cups[1].transform, rest[1]), "order switch returns Cup");
        Debug.Log("T13_AUTOMATED_PASS: four visible clickable Rack cups; existing tools remain clickable; ShakeComplete/Stable gating; all Serve poses; empty swaps; positive-volume rejection without mutation; R/order return; no preparation changes or Cup Flair");
    }
    static bool At(Transform t, Pose p) => Vector3.Distance(t.position,p.position)<.000001f && Quaternion.Angle(t.rotation,p.rotation)<.001f;
    static Bounds VisibleBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled && r.gameObject.activeInHierarchy).ToArray();
        Require(renderers.Length>0, "visible mesh " + go.name); var bounds=renderers[0].bounds;
        foreach(var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds); return bounds;
    }
    static void CheckVisible(Bounds bounds, string name)
    {
        for(int i=0;i<8;i++)
        {
            var p=Camera.main.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
            Require(p.z>Camera.main.nearClipPlane && p.x>0 && p.x<1 && p.y>0 && p.y<1, "outside viewport " + name + " " + p);
        }
    }
    static void CheckClickable(GameObject go, bool entireBounds = true)
    {
        Physics.SyncTransforms(); var bounds=VisibleBounds(go); if (entireBounds) CheckVisible(bounds,go.name);
        var center = Camera.main.WorldToViewportPoint(bounds.center);
        Require(center.z > Camera.main.nearClipPlane && center.x > 0 && center.x < 1 && center.y > 0 && center.y < 1, "click center outside viewport " + go.name);
        var ray=new Ray(Camera.main.transform.position,bounds.center-Camera.main.transform.position);
        bool hit=Physics.Raycast(ray,out var result,1000);
        Require(hit && result.transform.IsChildOf(go.transform), "ray blocked " + go.name + " by " + (hit?result.collider.name:"nothing"));
        if (entireBounds)
            foreach (var offset in new[] { new Vector2(.003f,0), new Vector2(-.003f,0), new Vector2(0,.003f), new Vector2(0,-.003f) })
            {
                var near = Camera.main.ViewportPointToRay(center + (Vector3)offset);
                Require(Physics.Raycast(near, out var adjacent, 1000) && adjacent.transform.IsChildOf(go.transform), "near-center click blocked " + go.name);
            }
    }
    static void CheckExisting()
    {
        int visibleBottles = 0;
        foreach(var bottle in UnityEngine.Object.FindObjectsByType<PourableIngredient>().Where(b=>b.isActiveAndEnabled))
        {
            var p = Camera.main.WorldToViewportPoint(VisibleBounds(bottle.gameObject).center);
            if(p.z <= 0 || p.x <= 0 || p.x >= 1 || p.y <= 0 || p.y >= 1) continue;
            CheckClickable(bottle.gameObject, false); visibleBottles++;
        }
        Require(visibleBottles >= 6, "six main bottles remain visible and clickable");
        var c=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>(); var so=new SerializedObject(c);
        CheckClickable(((DrinkContainer)so.FindProperty("jigger").objectReferenceValue).gameObject, false);
        CheckClickable(((ShakerPreparation)so.FindProperty("shaker").objectReferenceValue).gameObject, false);
        var well=(Transform)so.FindProperty("iceWell").objectReferenceValue; CheckClickable(well.gameObject, false);
    }
    static void Require(bool ok,string message) { if(!ok) throw new Exception("T13: "+message); }
    static void Log(string message,string stack,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) error=true; }
    static void Finish(int code)
    {
        Application.logMessageReceived-=Log; EditorApplication.update-=Tick; SessionState.SetBool(Key,false); SessionState.SetString("T13.Result",code==0?"PASS":"FAIL");
        if(ingredient) UnityEngine.Object.DestroyImmediate(ingredient);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.isPlaying=false;
    }
}
#endif
