#if UNITY_EDITOR
using System;
using CupPrototype.DrinkSystem;
using CupPrototype.Interaction;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class InteractionCoordinatorIceCheck
{
    const string BatchKey = "T07.ResetRegressionBatch";
    static int batchStep;
    static double batchDeadline;

    static InteractionCoordinatorIceCheck()
    {
        if (SessionState.GetBool(BatchKey, false)) HookBatch();
    }

    public static void RunBatch()
    {
        SessionState.SetBool(BatchKey, true);
        HookBatch();
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
    }

    static void HookBatch()
    {
        batchDeadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update -= TickBatch;
        EditorApplication.update += TickBatch;
    }

    static void TickBatch()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < batchDeadline, "batch timeout");
            if (!Application.isPlaying || Time.frameCount < 3) return;
            Application.runInBackground = true;
            EditorApplication.QueuePlayerLoopUpdate();
            if (batchStep == 0)
            {
                Run();
                InteractionCoordinatorMeasurementCheck.Run();
                InteractionCoordinatorShakerCheck.Run();
                InteractionCoordinatorJiggerCheck.Run();
                batchStep = 1;
            }
            else
            {
                string status = SessionState.GetString(batchStep == 1 ? "T03.JiggerCheck" : "T06.TransferCheck", "RUNNING");
                if (status == "RUNNING") return;
                Require(status.StartsWith("PASS"), status);
                if (batchStep == 1) { InteractionCoordinatorTransferCheck.Run(); batchStep = 2; }
                else { Debug.Log("T03_T07_RESET_REGRESSION_PASS"); FinishBatch(0); }
            }
        }
        catch (Exception error) { Debug.LogException(error); FinishBatch(1); }
    }

    static void FinishBatch(int code)
    {
        SessionState.SetBool(BatchKey, false);
        EditorApplication.update -= TickBatch;
        EditorApplication.Exit(code);
    }

    [MenuItem("Tools/CupPrototype/Validate T07 Process Ice (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying,"Run in Play Mode");
        var coordinator=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var shaker=UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        var record=UnityEngine.Object.FindAnyObjectByType<CurrentDrinkRecord>();
        var so=new SerializedObject(coordinator);
        var well=(Transform)so.FindProperty("iceWell").objectReferenceValue;
        var camera=Camera.main;
        Require(record && !record.ProcessIce && !shaker.ProcessIce && shaker.State==ShakerState.Rest,"initial facts");
        var containers=UnityEngine.Object.FindObjectsByType<DrinkContainer>(FindObjectsSortMode.None);
        var data=Array.ConvertAll(containers,c=>JsonUtility.ToJson(c));
        Physics.SyncTransforms();
        var ray=new Ray(camera.transform.position,well.position-camera.transform.position);
        Require(Physics.Raycast(ray,out var hit) && hit.collider.transform.IsChildOf(well),"Ice Well clickable from Camera");
        var collider=hit.collider;
        Require(!coordinator.TryAddProcessIce(collider) && !record.ProcessIce,"Rest rejection");
        Require(coordinator.TryAcquireShaker(shaker),"Preparing setup");
        foreach(var action in new[]{ActionState.AutoTransfer,ActionState.Flair,ActionState.Shake,ActionState.AutoPour})
        {
            coordinator.SetActionState(action);
            Require(!coordinator.TryAddProcessIce(collider) && !record.ProcessIce && !shaker.ProcessIce,"busy rejection");
        }
        coordinator.SetActionState(ActionState.Stable);
        Require(!coordinator.TryAddProcessIce(null) && !coordinator.TryAddProcessIce(shaker.GetComponentInChildren<Collider>()),"wrong target rejection");
        Require(coordinator.TryAddProcessIce(collider),"first Ice action");
        Require(record.ProcessIce && shaker.ProcessIce && coordinator.CurrentActionState==ActionState.Stable,"facts agree and Stable");
        var feedback=(GameObject)new SerializedObject(shaker).FindProperty("processIceFeedback").objectReferenceValue;
        Require(feedback && feedback.activeInHierarchy && feedback.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("PROCESS ICE"),"visible feedback");
        string first=JsonUtility.ToJson(record);
        for(int i=0;i<3;i++)Require(!coordinator.TryAddProcessIce(collider) && record.ProcessIce && shaker.ProcessIce && JsonUtility.ToJson(record)==first,"repeat does not record again");
        for(int i=0;i<containers.Length;i++)Require(JsonUtility.ToJson(containers[i])==data[i],"liquid/color/composition unchanged");
        shaker.ResetToRest();
        Require(record.ProcessIce && shaker.ProcessIce,"Shaker return does not clear attempt record");
        Require(!coordinator.TryAddProcessIce(collider),"Rest still rejects");
        var restPose=new Pose(shaker.transform.position,shaker.transform.rotation);
        Require(coordinator.TryAcquireShaker(shaker),"return to Preparing before R");
        // Invoke the same entry used by the R key, including its complete ResetCurrentDrink path.
        var manager=coordinator.GetComponent<DrinkTestManager>();
        var reset=typeof(DrinkTestManager).GetMethod("ClearCurrentDrink",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        reset.Invoke(manager,null);
        Require(!record.ProcessIce && !shaker.ProcessIce && !feedback.activeSelf,"R clears fact and immediately hides UI");
        Require(shaker.State==ShakerState.Rest && Vector3.Distance(shaker.transform.position,restPose.position)<.000001f && Quaternion.Angle(shaker.transform.rotation,restPose.rotation)<.001f,"R restores Rest pose");
        Require(!coordinator.TryAddProcessIce(collider),"R requires reacquire");
        Require(coordinator.TryAcquireShaker(shaker) && coordinator.TryAddProcessIce(collider),"new attempt can add ice again");
        Require(record.ProcessIce && shaker.ProcessIce && feedback.activeSelf,"second attempt UI follows fact");
        Require(!coordinator.TryAddProcessIce(collider),"second attempt still one-time");
        record.ResetAttempt();
        Require(!shaker.ProcessIce && !feedback.activeSelf,"direct data reset uses same UI refresh");
        Require(coordinator.TryAddProcessIce(collider) && feedback.activeSelf,"data add uses same UI refresh");
        reset.Invoke(manager,null);
        SessionState.SetString("T07.IceCheck","PASS: first/repeat/illegal Ice; liquid unchanged; normal Return preserves fact; R resets fact/UI/Rest; reacquire and add again; shared data-driven UI refresh");
        Debug.Log("T07_AUTOMATED_"+SessionState.GetString("T07.IceCheck",""));
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception("T07: "+message);}
}
#endif
