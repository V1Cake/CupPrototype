#if UNITY_EDITOR
using System;
using CupPrototype.DrinkSystem;
using CupPrototype.Interaction;
using UnityEditor;
using UnityEngine;

public static class InteractionCoordinatorShakerCheck
{
    [MenuItem("Tools/CupPrototype/Validate T05 Shaker (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying, "Run in Play Mode");
        var coordinator = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var shaker = UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        Require(coordinator && shaker, "scene references");
        Require(UnityEngine.Object.FindObjectsByType<ShakerPreparation>(FindObjectsSortMode.None).Length == 1, "one reusable Shaker");
        Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld && shaker.State == ShakerState.Rest, "initial state");
        var legacy = shaker.GetComponent<ShakerController>();
        Require(!legacy.enabled, "Acquire movement cannot trigger legacy Shake");
        var rest = new Pose(shaker.transform.position, shaker.transform.rotation);
        var scale = shaker.transform.localScale;
        var prep = (Transform)new SerializedObject(shaker).FindProperty("prepPosition").objectReferenceValue;
        var container = shaker.GetComponent<DrinkContainer>();
        string data = JsonUtility.ToJson(container);
        Require(!coordinator.TryAcquireShaker(null), "reject wrong target");
        coordinator.SetActionState(ActionState.AutoTransfer);
        Require(!coordinator.TryAcquireShaker(shaker), "reject busy action");
        coordinator.SetActionState(ActionState.Stable);
        var bottle = GameObject.Find("Bottle_Vodka").GetComponent<PourableIngredient>();
        Require(coordinator.TryAcquireBottle(bottle), "Bottle acquire");
        Require(!coordinator.TryAcquireShaker(shaker), "release occupied hand before Shaker acquire");
        coordinator.TryReturnHeld();
        for (int i = 0; i < 3; i++)
        {
            Require(coordinator.TryAcquireShaker(shaker), "Rest acquire");
            Require(shaker.State == ShakerState.Preparing && coordinator.CurrentActionState == ActionState.Stable, "completion states");
            Require(!coordinator.PrimaryHeld && !coordinator.SecondaryHeld, "no long-term Held Shaker");
            AtPose(shaker.transform, new Pose(prep.position, prep.rotation));
            Require(!coordinator.TryAcquireShaker(shaker) && !shaker.TryAcquire(), "repeat rejected");
            AtPose(shaker.transform, new Pose(prep.position, prep.rotation));
            Physics.SyncTransforms();
            var collider = shaker.GetComponentInChildren<Collider>();
            var ray = new Ray(Camera.main.transform.position, collider.bounds.center - Camera.main.transform.position);
            Require(Physics.Raycast(ray, out var hit) && hit.collider.GetComponentInParent<ShakerPreparation>() == shaker, "Prep reachable by camera ray");
            foreach(var renderer in shaker.GetComponentsInChildren<Renderer>())
            {
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                for(int k=0;k<8;k++)
                {
                    var corner=renderer.bounds.center+Vector3.Scale(renderer.bounds.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));
                    var v=Camera.main.WorldToViewportPoint(corner);
                    Require(v.x>0 && v.x<1 && v.y>0 && v.y<1 && v.z>Camera.main.nearClipPlane,"visible Prep bounds");
                }
            }
            Require(JsonUtility.ToJson(container)==data && shaker.transform.localScale==scale,"liquid/scale unchanged");
            shaker.ResetToRest();
            Require(shaker.State==ShakerState.Rest,"Reset state");
            AtPose(shaker.transform,rest);
            Require(JsonUtility.ToJson(container)==data,"Reset does not clear drink data");
        }
        Require(UnityEngine.Object.FindObjectsByType<ShakerPreparation>(FindObjectsSortMode.None).Length==1,"no duplicate instance");
        SessionState.SetString("T05.ShakerCheck","PASS: three Acquire/repeat/Reset cycles; Stable and empty hands; busy/occupied rejection; exact poses; visible/reachable Prep; no data changes");
        Debug.Log("T05_AUTOMATED_"+SessionState.GetString("T05.ShakerCheck",""));
    }
    static void AtPose(Transform target,Pose pose) => Require(Vector3.Distance(target.position,pose.position)<.000001f && Quaternion.Angle(target.rotation,pose.rotation)<.001f,"exact pose");
    static void Require(bool condition,string message){if(!condition)throw new Exception("T05: "+message);}
}
#endif
