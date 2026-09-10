#if UNITY_EDITOR
using System;
using CupPrototype.DrinkSystem;
using CupPrototype.Interaction;
using UnityEditor;
using UnityEngine;

public static class InteractionCoordinatorTransferCheck
{
    static InteractionCoordinator coordinator;
    static ShakerPreparation shaker;
    static DrinkContainer jigger, target;
    static IngredientData ingredient;
    static PourableIngredient bottle;
    static bool dualPhase;
    static string bottleData;
    static Transform anchor;
    static int servings, completed;
    static float expected, portion;
    static double deadline;
    static bool previousBackground;

    [MenuItem("Tools/CupPrototype/Validate T06 AutoTransfer (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying,"Play Mode required");
        coordinator=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        shaker=UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        var so=new SerializedObject(coordinator);
        jigger=(DrinkContainer)so.FindProperty("jigger").objectReferenceValue;
        anchor=(Transform)so.FindProperty("jiggerHoldAnchor").objectReferenceValue;
        target=shaker.GetComponent<DrinkContainer>();
        Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld && jigger.IsEmpty() && target.IsEmpty() && shaker.State==ShakerState.Rest,"initial empty state");
        bottle=GameObject.Find("Bottle_Vodka").GetComponent<PourableIngredient>();
        bottleData=JsonUtility.ToJson(bottle);
        dualPhase=false;
        ingredient=bottle.ingredientData;
        portion=jigger.MaxVolume;
        servings=Mathf.CeilToInt(target.MaxVolume/portion)+2;
        Require(servings<50 && target.UnlimitedReceive,"configured Unlimited Receive");
        jigger.AddIngredient(ingredient,portion,false);
        Require(coordinator.TryAcquireJigger(jigger),"Jigger acquire");
        Require(!coordinator.TryStartJiggerTransfer(shaker) && target.IsEmpty() && jigger.CurrentVolume==portion,"Rest rejection without mutation");
        coordinator.TryReturnHeld();
        Require(coordinator.TryAcquireShaker(shaker) && coordinator.TryAcquireJigger(jigger),"Preparing setup");
        coordinator.SetActionState(ActionState.AutoPour);
        Require(!coordinator.TryStartJiggerTransfer(shaker) && target.IsEmpty() && jigger.CurrentVolume==portion,"busy rejected");
        coordinator.SetActionState(ActionState.Stable);
        Require(!coordinator.TryStartJiggerTransfer(null),"wrong target rejected");
        expected=0;completed=0;deadline=EditorApplication.timeSinceStartup+60;
        previousBackground=Application.runInBackground;Application.runInBackground=true;
        SessionState.SetString("T06.TransferCheck","RUNNING");
        Application.logMessageReceived+=OnLog;
        EditorApplication.update+=Tick;
        StartPortion();
    }
    static void StartPortion()
    {
        Require(coordinator.TryStartJiggerTransfer(shaker),"one-click start");
        Require(coordinator.CurrentActionState==ActionState.AutoTransfer,"busy throughout animation");
        Require(!coordinator.TryReturnHeld() && !coordinator.TryStartJiggerTransfer(shaker),"RMB/reentry rejected");
    }
    static void FillJigger()
    {
        if (!dualPhase) {jigger.AddIngredient(ingredient,portion,false);return;}
        Physics.SyncTransforms();
        var camera=Camera.main;
        var ray=new Ray(camera.transform.position,jigger.GetComponentInChildren<Collider>().bounds.center-camera.transform.position);
        Require(coordinator.TryStartMeasurement(ray),"Measurement resumes without putting Bottle down");
        coordinator.AdvanceMeasurement(true,portion/bottle.pourRatePerSecond);
        coordinator.AdvanceMeasurement(false,0);
        Require(coordinator.CurrentActionState==ActionState.Stable && Mathf.Abs(jigger.CurrentVolume-portion)<.001f,"Measurement amount/stop");
    }
    static void Tick()
    {
        try
        {
            Require(Application.isPlaying && EditorApplication.timeSinceStartup<deadline,"timeout or Play stopped");
            EditorApplication.QueuePlayerLoopUpdate();
            if(coordinator.CurrentActionState==ActionState.AutoTransfer)return;
            expected+=portion;completed++;
            Require(coordinator.CurrentActionState==ActionState.Stable,"callback Stable");
            Require(dualPhase ? coordinator.PrimaryHeld==bottle.gameObject && coordinator.SecondaryHeld==jigger.gameObject
                : coordinator.PrimaryHeld==jigger.gameObject && !coordinator.SecondaryHeld,"callback Held references");
            Require(JsonUtility.ToJson(bottle)==bottleData,"Bottle remains unchanged");
            Require(jigger.IsEmpty() && jigger.Ingredients.Count==0 && Mathf.Abs(target.CurrentVolume-expected)<.001f,"full transfer/conservation");
            Require(target.Ingredients.Count==1 && target.Ingredients[0].ingredient==ingredient && Mathf.Abs(target.Ingredients[0].amount-expected)<.001f,"ingredient conservation");
            Require(Vector3.Distance(jigger.transform.position,anchor.position)<.000001f && Quaternion.Angle(jigger.transform.rotation,anchor.rotation)<.001f,"return Hold pose");
            Require(!coordinator.TryStartJiggerTransfer(shaker) && target.CurrentVolume==expected,"empty repeat no mutation");
            if(completed<servings){FillJigger();StartPortion();return;}
            Require(target.CurrentVolume>target.MaxVolume,"exceeds real maxVolume");
            if(!dualPhase)
            {
                coordinator.TryReturnHeld();
                Require(coordinator.TryAcquireBottle(bottle),"dual phase Bottle acquire");
                Require(!coordinator.TryStartJiggerTransfer(shaker) && target.CurrentVolume==expected,"Bottle alone cannot transfer");
                Require(coordinator.TryAcquireJigger(jigger),"dual phase Jigger acquire");
                dualPhase=true;completed=0;servings=3;
                FillJigger();StartPortion();return;
            }
            target.Clear();
            // Disabling the flag restores the original bounded TransferTo path.
            var so=new SerializedObject(target);
            float capacity=target.MaxVolume;
            so.FindProperty("unlimitedReceive").boolValue=false;so.FindProperty("maxVolume").floatValue=1;so.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                jigger.AddIngredient(ingredient,portion,false);
                Require(jigger.TransferTo(target,portion),"finite transfer");
                Require(Mathf.Abs(target.CurrentVolume-1)<.001f && Mathf.Abs(jigger.CurrentVolume-(portion-1))<.001f,"finite capacity restored");
                Require(!jigger.TransferTo(target,portion),"full finite target rejects");
            }
            finally {so.Update();so.FindProperty("unlimitedReceive").boolValue=true;so.FindProperty("maxVolume").floatValue=capacity;so.ApplyModifiedPropertiesWithoutUndo();}
            jigger.Clear();target.Clear();coordinator.TryReturnHeld();coordinator.TryReturnHeld();shaker.ResetToRest();
            Finish("PASS: Primary Jigger regression + 3 consecutive dual-held Measurement/Transfer cycles; Bottle unchanged; conservation beyond maxVolume; callback/Held/pose; empty/Bottle-only rejection; finite capacity fallback");
        }
        catch(Exception e){Finish("FAIL: "+e.Message);}
    }
    static void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish("FAIL: "+message);}
    static void Finish(string result)
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;Application.runInBackground=previousBackground;
        SessionState.SetString("T06.TransferCheck",result);Debug.Log("T06_AUTOMATED_"+result);
    }
    static void Require(bool value,string message){if(!value)throw new Exception("T06: "+message);}
}
#endif
