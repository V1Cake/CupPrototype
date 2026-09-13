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

[InitializeOnLoad]
public static class InteractionDualIceCheck
{
    const string Key = "T14.Running";
    static IEnumerator checks;
    static double deadline;
    static bool error;
    static IngredientData ingredient;
    static InteractionDualIceCheck() { if(SessionState.GetBool(Key,false)) Hook(); }
    public static void SetupAndRun()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"save Scene before setup");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var c=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>(); var so=new SerializedObject(c);
        var shake=(Transform)so.FindProperty("iceWell").objectReferenceValue; shake.name="Shake Ice Well"; shake.position=new Vector3(-.43f,.91f,1.01f);
        var serve=(Transform)so.FindProperty("serveIceWell").objectReferenceValue;
        if(!serve) serve=UnityEngine.Object.Instantiate(shake.gameObject,shake.parent).transform;
        serve.name="Serve Ice Well"; serve.position=new Vector3(-.64f,.91f,.68f);
        so.FindProperty("serveIceWell").objectReferenceValue=serve; so.ApplyModifiedPropertiesWithoutUndo();
        var ui=new SerializedObject(c.GetComponent<ShakerContextUI>());
        MakeLabel(ui,"shakeIceLabel",shake,"SHAKE ICE"); MakeLabel(ui,"serveIceLabel",serve,"SERVE ICE");
        ui.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene); EditorSceneManager.SaveScene(c.gameObject.scene); RunBatch();
    }
    static void MakeLabel(SerializedObject ui,string property,Transform well,string title)
    {
        var text=(TMP_Text)ui.FindProperty(property).objectReferenceValue;
        if(!text) text=new GameObject(title+" Label").AddComponent<TextMeshPro>();
        text.font=TMP_Settings.defaultFontAsset; text.text=title; text.fontSize=.6f;
        text.alignment=TextAlignmentOptions.Center; text.color=Color.white;
        text.rectTransform.sizeDelta=new Vector2(.5f,.14f);
        var anchor=text.transform.parent;
        if(!anchor) anchor=new GameObject(title+" Label Anchor").transform;
        anchor.SetPositionAndRotation(well.position+new Vector3(0,.20f,.02f),Camera.main.transform.rotation);
        text.transform.SetParent(anchor,false); text.rectTransform.anchoredPosition3D=Vector3.zero; text.transform.localRotation=Quaternion.identity;
        ui.FindProperty(property).objectReferenceValue=text;
    }
    public static void RunBatch()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"save Scene before check");
        if(Application.isBatchMode) PlayModeWindow.SetCustomRenderingResolution(1920,1080,"Interaction Check");
        checks=null; error=false; SessionState.SetBool(Key,true); Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); EditorApplication.isPlaying=true;
    }
    static void Hook() { deadline=EditorApplication.timeSinceStartup+120; EditorApplication.update-=Tick; EditorApplication.update+=Tick; }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"timeout"); if(!Application.isPlaying)return;
            Application.runInBackground=true; EditorApplication.QueuePlayerLoopUpdate(); if(Time.frameCount<3)return;
            if(checks==null){Application.logMessageReceived+=Log;checks=RunChecks();}
            Require(!error,"runtime Error / Exception"); if(!checks.MoveNext())Finish(0);
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static IEnumerator RunChecks()
    {
        var c=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>(); var r=c.GetComponent<CurrentDrinkRecord>();
        var s=UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>(); var so=new SerializedObject(c);
        var shake=(Transform)so.FindProperty("iceWell").objectReferenceValue; var serve=(Transform)so.FindProperty("serveIceWell").objectReferenceValue;
        var a=shake.GetComponent<Collider>(); var b=serve.GetComponent<Collider>();
        var list=so.FindProperty("rackCups"); var cups=Enumerable.Range(0,list.arraySize).Select(i=>(DrinkContainer)list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        Require(shake!=serve && a && b,"two separate physical sources"); CheckClick(a); CheckClick(b);
        Require(!c.TryAddProcessIce(a)&&!c.TryAddServeIce(b)&&!r.ProcessIce&&!r.ServeIce,"no targets rejects both");
        Require(c.TryAcquireCup(cups[0])&&c.TryAddServeIce(b),"Rest empty Cup accepts Serve Ice");
        Require(r.ServeIce&&!r.ProcessIce&&r.PreparationVersion==0&&!r.ShakePerformed,"Serve Ice only");
        CheckLabel(c,"serveIceLabel","SERVE ICE\nADDED");
        Require(!c.TryAddServeIce(b)&&!c.TryAddServeIce(a)&&!c.TryAddProcessIce(b),"repeat/wrong source reject");
        Require(c.TryAcquireCup(cups[1])&&!r.ServeIce,"empty iced Cup replacement clears Ice");
        CheckLabel(c,"serveIceLabel","SERVE ICE");
        Require(c.TryAcquireShaker(s)&&c.TryAddProcessIce(a),"Preparing Process Ice");
        int version=r.PreparationVersion;
        Require(r.ProcessIce&&!r.ServeIce&&!c.TryAddProcessIce(a)&&r.PreparationVersion==version,"Process once only");
        Require(c.TryAddServeIce(b)&&r.ProcessIce&&r.ServeIce&&r.PreparationVersion==version,"both independent in Preparing");
        var j=(DrinkContainer)so.FindProperty("jigger").objectReferenceValue; var drink=s.GetComponent<DrinkContainer>();
        ingredient=ScriptableObject.CreateInstance<IngredientData>(); ingredient.sourness=100;
        j.AddIngredient(ingredient,10,false); Require(j.TransferTo(drink,10),"Taste fixture");
        string contents=JsonUtility.ToJson(drink);
        var stick=(StirStickTaste)so.FindProperty("stirStick").objectReferenceValue;
        Require(!r.HasShaken&&c.TryTaste(stick.GetComponent<Collider>()),"Taste before Shake with both Ice facts");
        while(c.CurrentActionState!=ActionState.Stable)yield return null;
        Require(r.ProcessIce&&r.ServeIce&&!r.ShakePerformed&&JsonUtility.ToJson(drink)==contents,"Taste preserves Ice and liquid");
        Require(c.TryAcquireCup(cups[2])&&!r.ServeIce&&r.ProcessIce&&!r.CanTasteCurrentVersion,"swap clears only Serve Ice");
        c.SetActionState(ActionState.AutoPour); Require(!c.TryAddServeIce(b)&&!c.TryAddProcessIce(a),"Busy rejects Ice"); c.SetActionState(ActionState.Stable);
        Require(c.TryCloseShaker(),"Close"); while(c.CurrentActionState!=ActionState.Stable)yield return null;
        Require(!c.TryAddProcessIce(a)&&c.TryAddServeIce(b),"Ready Shaker does not constrain Serve Ice");
        Require(c.TryBeginShakeGesture(s.GetComponent<FlairableTool>())&&c.CompleteShakeGesture(new GestureMatchResult{isMatched=true,templateId="ShakerRoll_01",gestureType=FlairGestureType.Circle}),"Shake");
        Require(!c.TryAddServeIce(b),"Shake animation locks Ice"); while(c.CurrentActionState!=ActionState.Stable)yield return null;
        Require(c.TryAcquireCup(cups[0])&&c.TryAddServeIce(b)&&r.ShakePerformed&&r.PreparationVersion==version,"ShakeComplete Serve Ice leaves Shake/version alone");
        Require(c.TryAcquireCup(cups[1]),"swap again"); cups[1].AddIngredient(ingredient,.001f,false);
        string cupData=JsonUtility.ToJson(cups[1]);
        Require(!c.TryAddServeIce(b)&&!c.TryAcquireCup(cups[2])&&JsonUtility.ToJson(cups[1])==cupData&&!r.ServeIce,"nonempty rejects Ice and swap without mutation");
        typeof(DrinkTestManager).GetMethod("ClearCurrentDrink",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c.GetComponent<DrinkTestManager>(),null);
        Require(!r.ServeIce&&!r.ProcessIce&&!r.SelectedGlass,"R clears both");
        CheckLabel(c,"serveIceLabel","SERVE ICE");CheckLabel(c,"shakeIceLabel","SHAKE ICE");
        Require(c.TryAcquireCup(cups[0])&&c.TryAddServeIce(b),"new attempt Serve Ice");
        UnityEngine.Object.FindAnyObjectByType<DemoRoundManager>().StartNextTarget();
        Require(!r.ServeIce&&!r.ProcessIce&&!r.SelectedGlass,"order switch clears both");
        Debug.Log("T14_AUTOMATED_PASS: independent physical sources; early Cup/Serve Ice; valid/invalid/repeated/busy targets; swap clears Serve Ice only; nonempty rejection; Taste before Shake preserves both Ice facts; R/order cleanup; labels follow data");
    }
    static void CheckLabel(InteractionCoordinator c,string property,string value)
    {
        var ui=c.GetComponent<ShakerContextUI>();ui.Refresh();
        var label=(TMP_Text)new SerializedObject(ui).FindProperty(property).objectReferenceValue;
        Require(label.text==value,"data-driven "+property);
        var well=(Transform)new SerializedObject(c).FindProperty(property=="serveIceLabel"?"serveIceWell":"iceWell").objectReferenceValue;
        Require(Vector3.Distance(label.transform.position,well.position+new Vector3(0,.20f,.02f))<.001f,"label stays above its source after Scene reload");
    }
    static void CheckClick(Collider collider)
    {
        Physics.SyncTransforms();var p=Camera.main.WorldToViewportPoint(collider.bounds.center);
        Require(p.z>Camera.main.nearClipPlane&&p.x>.05f&&p.x<.95f&&p.y>.05f&&p.y<.95f,"source visible "+collider.name);
        foreach(var delta in new[]{Vector2.zero,new Vector2(.003f,0),new Vector2(-.003f,0),new Vector2(0,.003f),new Vector2(0,-.003f)})
            Require(Physics.Raycast(Camera.main.ViewportPointToRay(p+(Vector3)delta),out var hit,1000)&&hit.collider.transform.IsChildOf(collider.transform),"source stable ray "+collider.name);
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception("T14: "+message);}
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)error=true;}
    static void Finish(int code)
    {
        Application.logMessageReceived-=Log;EditorApplication.update-=Tick;SessionState.SetBool(Key,false);SessionState.SetString("T14.Result",code==0?"PASS":"FAIL");
        if(ingredient)UnityEngine.Object.DestroyImmediate(ingredient);
        if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.isPlaying=false;
    }
}
#endif
