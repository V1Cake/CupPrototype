#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.Game;
using CupPrototype.Interaction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FinishedDrinkSubmitCheck
{
    const string Key="T16.Running";
    static IEnumerator checks;
    static double deadline;
    static bool error;
    static IngredientData a,b;
    static FinishedDrinkSubmitCheck(){if(SessionState.GetBool(Key,false))Hook();}
    public static void SetupAndRun()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"saved Edit Mode");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var c=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();var so=new SerializedObject(c);
        var anchor=Camera.main.transform.Find("FinalCupHoldAnchor");
        if(!anchor){anchor=new GameObject("FinalCupHoldAnchor").transform;anchor.SetParent(Camera.main.transform,false);}
        anchor.localPosition=new Vector3(.24f,-.19f,.45f);anchor.rotation=Quaternion.identity;
        var zone=GameObject.Find("Drink Service Zone");
        if(!zone){zone=GameObject.CreatePrimitive(PrimitiveType.Cube);zone.name="Drink Service Zone";}
        zone.transform.position=new Vector3(.20f,.916f,.65f);zone.transform.localScale=new Vector3(.14f,.006f,.12f);
        zone.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Glassware/Common/Materials/M_CupLiquid_Base.mat");
        var position=GameObject.Find("DrinkServicePosition")?.transform;
        if(!position)position=new GameObject("DrinkServicePosition").transform;
        position.position=zone.transform.position+Vector3.up*.004f;
        var labelAnchor=GameObject.Find("Service Label Anchor")?.transform;
        if(!labelAnchor)
        {
            labelAnchor=new GameObject("Service Label Anchor").transform;
            var text=new GameObject("Service Label").AddComponent<TextMeshPro>();text.transform.SetParent(labelAnchor,false);
            text.font=TMP_Settings.defaultFontAsset;text.fontSize=.35f;text.alignment=TextAlignmentOptions.Center;text.text="SERVE";
            text.rectTransform.sizeDelta=new Vector2(.15f,.04f);text.rectTransform.anchoredPosition3D=Vector3.zero;
        }
        labelAnchor.SetPositionAndRotation(zone.transform.position+new Vector3(0,.035f,.055f),Camera.main.transform.rotation);
        so.FindProperty("finalHoldAnchor").objectReferenceValue=anchor;so.FindProperty("serviceZone").objectReferenceValue=zone.GetComponent<Collider>();
        so.FindProperty("servicePosition").objectReferenceValue=position;so.FindProperty("roundManager").objectReferenceValue=UnityEngine.Object.FindAnyObjectByType<DemoRoundManager>();
        so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);RunBatch();
    }
    public static void RunBatch(){checks=null;error=false;SessionState.SetBool(Key,true);Hook();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;}
    static void Hook(){deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"timeout");if(!Application.isPlaying)return;
            Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();if(Time.frameCount<3)return;
            if(checks==null){Application.logMessageReceived+=Log;checks=RunChecks();}Require(!error,"runtime Error/Exception");if(!checks.MoveNext())Finish(0);
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static IEnumerator RunChecks()
    {
        var c=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();var r=c.GetComponent<CurrentDrinkRecord>();
        var round=UnityEngine.Object.FindAnyObjectByType<DemoRoundManager>();var so=new SerializedObject(c);
        var zone=(Collider)so.FindProperty("serviceZone").objectReferenceValue;
        var dest=(Transform)so.FindProperty("servicePosition").objectReferenceValue;
        var j=(DrinkContainer)so.FindProperty("jigger").objectReferenceValue;
        var s=UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();var source=s.GetComponent<DrinkContainer>();
        a=ScriptableObject.CreateInstance<IngredientData>();b=ScriptableObject.CreateInstance<IngredientData>();a.sourness=31;b.sweetness=74;
        for(int index=0;index<3;index++)
        {
            var cup=(DrinkContainer)so.FindProperty("rackCups").GetArrayElementAtIndex(index).objectReferenceValue;
            Require(!c.TrySubmitCup(zone),"no Cup rejected");Require(c.TryAcquireCup(cup),"select Cup");
            Require(!c.TryAcquireFinishedCup(cup) && !c.TrySubmitCup(zone),"empty rejected");
            cup.AddIngredient(a,1,false);Require(!c.TryAcquireFinishedCup(cup),"liquid without completed Pour rejected");cup.Clear();
            Require(c.TryAcquireShaker(s),"Shaker");
            j.AddIngredient(a,20,false);Require(j.TransferTo(source,20),"first ingredient");j.AddIngredient(b,20,false);Require(j.TransferTo(source,20),"second ingredient");
            Require(c.TryAddProcessIce(((Transform)so.FindProperty("iceWell").objectReferenceValue).GetComponent<Collider>()),"process ice");
            Require(c.TryAddServeIce(((Transform)so.FindProperty("serveIceWell").objectReferenceValue).GetComponent<Collider>()),"serve ice");r.RecordTaste("fixture");
            Require(c.TryCloseShaker(),"Close");while(c.CurrentActionState!=ActionState.Stable)yield return null;
            Require(c.TryBeginShakeGesture(s.GetComponent<FlairableTool>()) && c.CompleteShakeGesture(new GestureMatchResult{isMatched=true,templateId="ShakerRoll_01",gestureType=FlairGestureType.Circle}),"Shake");
            while(c.CurrentActionState!=ActionState.Stable)yield return null;
            Require(c.TryStartAutoPour(cup) && !c.TryAcquireFinishedCup(cup) && !c.TrySubmitCup(zone),"busy rejected");
            while(c.CurrentActionState!=ActionState.Stable)yield return null;
            var order=r.ActiveOrder;string data=JsonUtility.ToJson(cup);var pose=new Pose(cup.transform.position,cup.transform.rotation);
            Require(c.TryAcquireFinishedCup(cup),"Final Held");CheckRay(zone);Capture(-index-1);
            Require(c.TryReturnHeld() && Vector3.Distance(cup.transform.position,pose.position)<.001f,"Final Held RMB to Serve Position");
            Require(c.TryAcquireFinishedCup(cup) && !c.TrySubmitCup(cup.GetComponent<Collider>()),"wrong destination rejected");
            Require(!round.TrySubmitFinishedDrink(new OrderContext(order.Recipe),r,cup) && !r.Submitted,"wrong order rejected without mutation");
            Require(c.TrySubmitCup(zone),"submit once");
            Require(r.Submitted && r.PourCompleted && r.ProcessIce && r.ServeIce && r.Tasted && r.SelectedGlass==cup && !c.PrimaryHeld,"facts preserved / Held clear");
            Require(round.FinishedDrink==cup && round.SubmittedRecord==r && ReferenceEquals(round.ActiveOrder,order) && round.State==DemoRoundState.Submitted,"correct order/record/actual Cup handed off");
            Require(JsonUtility.ToJson(cup)==data,"submission does not change drink");
            var expected=DrinkScoreSystem.ScoreDrink(cup,order.Recipe);
            Require(Mathf.Abs(c.GetComponent<DrinkTestManager>().LastEvaluation.totalScore-expected.totalScore)<.0001f,"existing score event uses actual Cup and order");
            Require(!c.TrySubmitCup(zone) && !c.TryAcquireFinishedCup(cup) && !c.TryAcquireCup(cup) && !c.TryAcquireShaker(s) && !c.TryStartAutoPour(cup) && !c.TryAddServeIce(((Transform)so.FindProperty("serveIceWell").objectReferenceValue).GetComponent<Collider>()),"submitted blocks edits/repeats");
            Require(Vector3.Distance(cup.transform.position,pose.position)>.05f,"Cup moved to service");
            var bounds=cup.GetComponentInChildren<Renderer>().bounds;
            foreach(var renderer in cup.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
            Require(Vector3.Distance(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),dest.position)<.002f,"Cup bottom aligned to fixed service position");
            Capture(index);
            round.StartNextTarget();Require(!r.Submitted && !r.SelectedGlass && !round.FinishedDrink && cup.IsEmpty(),"next order lifecycle reset");
        }
        for(int reset=0;reset<2;reset++)
        {
            var cup=(DrinkContainer)so.FindProperty("rackCups").GetArrayElementAtIndex(0).objectReferenceValue;
            Require(c.TryAcquireCup(cup),"reset fixture Cup");cup.AddIngredient(a,10,false);r.RecordPour();
            Require(c.TryAcquireFinishedCup(cup),"unsubmitted Final Held reset fixture");
            if(reset==0)typeof(DrinkTestManager).GetMethod("ClearCurrentDrink",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c.GetComponent<DrinkTestManager>(),null);
            else round.StartNextTarget();
            Require(!c.PrimaryHeld && !r.Submitted && !r.SelectedGlass && cup.IsEmpty() && c.CurrentActionState==ActionState.Stable,"R/order clears unsubmitted Final Held");
        }
        Debug.Log("T16_AUTOMATED_PASS: three Cup submissions; empty/unfinished/busy/wrong/repeat rejection; actual Cup/order/record scoring; facts; Held return; next order");
    }
    static void CheckRay(Collider collider)
    {
        Physics.SyncTransforms();var ray=Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(collider.bounds.center));
        Require(Physics.Raycast(ray,out var hit,1000) && hit.collider==collider,"Service Zone clickable during Final Held");
    }
    static void Capture(int index)
    {
        var cam=Camera.main;var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"T16-Submit-"+index+".png"),tex.EncodeToPNG());}
        finally{cam.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception("T16: "+message);}
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)error=true;}
    static void Finish(int code){Application.logMessageReceived-=Log;EditorApplication.update-=Tick;SessionState.SetBool(Key,false);if(a)UnityEngine.Object.DestroyImmediate(a);if(b)UnityEngine.Object.DestroyImmediate(b);if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.isPlaying=false;}
}
#endif
