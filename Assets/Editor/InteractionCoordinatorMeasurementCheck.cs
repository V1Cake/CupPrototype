#if UNITY_EDITOR
using System;
using System.Linq;
using CupPrototype.DrinkSystem;
using CupPrototype.Interaction;
using CupPrototype.UI;
using UnityEditor;
using UnityEngine;

public static class InteractionCoordinatorMeasurementCheck
{
    [MenuItem("Tools/CupPrototype/Validate T04 Measurement (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying, "Run in Play Mode");
        var coordinator = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var serialized = new SerializedObject(coordinator);
        var jigger = (DrinkContainer)serialized.FindProperty("jigger").objectReferenceValue;
        var anchor = (Transform)serialized.FindProperty("bottleHoldAnchor").objectReferenceValue;
        var overlay = (MeasurementOverlay)serialized.FindProperty("measurementOverlay").objectReferenceValue;
        var cameraLock = (MeasurementCameraLock)serialized.FindProperty("measurementCamera").objectReferenceValue;
        var camera = Camera.main;
        Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld && jigger.CurrentVolume == 0, "empty hands / Jigger");
        var bottles = new[]{"Vodka","Gin","Rum","Tequila","Whiskey","Brandy"}
            .Select(n => GameObject.Find("Bottle_"+n).GetComponent<PourableIngredient>()).ToArray();
        var rest = bottles.Select(b => new Pose(b.transform.position,b.transform.rotation)).ToArray();
        var jiggerRest = new Pose(jigger.transform.position,jigger.transform.rotation);
        string failure = null;
        Application.LogCallback onLog = (message,stack,type) => {
            if(type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failure = message;
        };
        Application.logMessageReceived += onLog;
        try
        {
            for(int i=0;i<bottles.Length;i++)
            {
                var bottle = bottles[i];
                string sourceData = JsonUtility.ToJson(bottle);
                Require(coordinator.TryAcquireJigger(jigger), "solo Jigger acquire");
                Require(coordinator.TryAcquireBottle(bottle), "reverse Bottle acquire");
                Require(coordinator.PrimaryHeld==bottle.gameObject && coordinator.SecondaryHeld==jigger.gameObject,"dual references");
                Require(!coordinator.TryAcquireBottle(bottles[(i+1)%bottles.Length]),"reject occupied swap");
                Physics.SyncTransforms();
                Require(!coordinator.TryStartMeasurement(new Ray(camera.transform.position,Vector3.up)),"reject miss");
                foreach(var other in UnityEngine.Object.FindObjectsByType<DrinkContainer>().Where(c=>c!=jigger))
                    Require(!coordinator.TryStartMeasurement(Aim(camera,other.gameObject)),"reject other container");
                Require(!coordinator.TryStartMeasurement(Aim(camera,bottle.gameObject)),"reject Bottle aim");
                var ray = Aim(camera,jigger.gameObject);
                Require(coordinator.TryStartMeasurement(ray),"start aimed Measurement "+bottle.name);
                Require(overlay.IsVisible && cameraLock.IsLocked && coordinator.CurrentActionState==ActionState.Measurement,"start state/UI/lock");
                Require(!coordinator.TryReturnHeld(),"RMB cannot interrupt");
                var position = camera.transform.position;
                camera.transform.position += Vector3.one;
                cameraLock.SendMessage("LateUpdate");
                Require(camera.transform.position==position,"camera pose locked");
                var terminal = UnityEngine.Object.FindAnyObjectByType<RecipeTerminalPrototype>();
                if(terminal){terminal.OpenBrowser();Require(!terminal.IsBrowserOpen,"recipe lock gate");}
                var focus = UnityEngine.Object.FindAnyObjectByType<DisplayFocusPrototype>();
                if(focus){focus.SetFocused(true);Require(!focus.IsFocused,"focus lock gate");}
                float amount = jigger.MaxVolume*.25f;
                coordinator.AdvanceMeasurement(true,amount/bottle.pourRatePerSecond);
                Require(Mathf.Abs(jigger.CurrentVolume-amount)<.001f,"fixed rate amount");
                Require(jigger.Ingredients.Count==1 && jigger.Ingredients[0].ingredient==bottle.ingredientData && Mathf.Abs(jigger.Ingredients[0].amount-amount)<.001f,"ingredient record");
                Require(Mathf.Abs(overlay.DisplayedFraction-.25f)<.001f,"realtime liquid view");
                coordinator.AdvanceMeasurement(false,1);
                Require(jigger.CurrentVolume==amount && !overlay.IsVisible && !cameraLock.IsLocked && coordinator.CurrentActionState==ActionState.Stable,"release stops/unlocks");
                AtPose(bottle.transform,new Pose(anchor.position,anchor.rotation));
                Physics.SyncTransforms();
                Require(coordinator.TryStartMeasurement(Aim(camera,jigger.gameObject)),"resume same material");
                coordinator.AdvanceMeasurement(true,jigger.MaxVolume/bottle.pourRatePerSecond*2);
                Require(jigger.IsFull() && Mathf.Abs(jigger.CurrentVolume-jigger.MaxVolume)<.001f,"capacity clamp");
                Require(coordinator.CurrentActionState==ActionState.Stable && !overlay.IsVisible && !cameraLock.IsLocked,"full auto stop");
                coordinator.AdvanceMeasurement(true,100);
                Require(!coordinator.TryStartMeasurement(Aim(camera,jigger.gameObject)),"full cannot restart");
                Require(sourceData==JsonUtility.ToJson(bottle),"unlimited source unchanged");
                Require(coordinator.TryReturnHeld(),"Bottle return");AtPose(bottle.transform,rest[i]);
                Require(coordinator.PrimaryHeld==jigger.gameObject && !coordinator.SecondaryHeld,"Jigger remains held");
                Require(coordinator.TryReturnHeld(),"Jigger return");AtPose(jigger.transform,jiggerRest);
                Require(jigger.IsFull(),"return preserves liquid");
                jigger.Clear();
            }
            Require(failure==null,"no runtime Error/Exception: "+failure);
            SessionState.SetString("T04.MeasurementCheck","PASS: six bottles; reverse acquire; ray gates; exact ingredient/rate/capacity; release/full stops; overlay; camera/UI gates; unlimited source; exact Return");
            Debug.Log("T04_AUTOMATED_"+SessionState.GetString("T04.MeasurementCheck",""));
        }
        catch(Exception error){SessionState.SetString("T04.MeasurementCheck","FAIL: "+error.Message);throw;}
        finally {Application.logMessageReceived-=onLog;coordinator.EndMeasurement();}
    }
    static Ray Aim(Camera camera,GameObject target)
    {
        var collider=target.GetComponentInChildren<Collider>();
        return new Ray(camera.transform.position,collider.bounds.center-camera.transform.position);
    }
    static void AtPose(Transform target,Pose pose) => Require(Vector3.Distance(target.position,pose.position)<.000001f && Quaternion.Angle(target.rotation,pose.rotation)<.001f,"exact Return/Hold pose");
    static void Require(bool condition,string message){if(!condition)throw new Exception("T04: "+message);}
}
#endif
