#if UNITY_EDITOR
using System;
using System.Linq;
using CupPrototype.DrinkSystem;
using CupPrototype.Interaction;
using CupPrototype.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class InteractionCoordinatorCloseCheck
{
    const string Key="T08.CloseBatch";
    static int step;
    static double deadline;
    static InteractionCoordinator coordinator;
    static ShakerPreparation shaker;
    static ShakerContextUI ui;
    static DrinkContainer container;
    static Transform lid;
    static Vector3 closedPosition;
    static string liquid;
    static bool ice;
    static GameObject primary,secondary;
    static InteractionCoordinatorCloseCheck(){if(SessionState.GetBool(Key,false))Hook();}

    public static void SetupAndRun()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"save Scene and leave Play Mode before setup");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var c=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var s=UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
        var tin=s.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Shaker_SmallTin");
        var large=s.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Shaker_LargeTin");
        var small=tin.GetComponent<Renderer>();
        var so=new SerializedObject(s);
        so.FindProperty("lid").objectReferenceValue=tin;
        so.FindProperty("openLidLocalPosition").vector3Value=tin.localPosition+tin.parent.InverseTransformVector(new Vector3(-.13f,large.bounds.min.y-small.bounds.min.y,0));
        so.ApplyModifiedPropertiesWithoutUndo();
        var canvas=GameObject.Find("ProcessIceFeedbackCanvas");
        if(!canvas.GetComponent<GraphicRaycaster>())canvas.AddComponent<GraphicRaycaster>();
        var buttonObj=new GameObject("CloseShakerButton",typeof(RectTransform),typeof(Image),typeof(Button));
        var rect=buttonObj.GetComponent<RectTransform>();rect.SetParent(canvas.transform,false);
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.21f);rect.sizeDelta=new Vector2(310,54);
        buttonObj.GetComponent<Image>().color=new Color(.045f,.085f,.102f,.98f);
        var textObj=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
        var tr=textObj.GetComponent<RectTransform>();tr.SetParent(rect,false);tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
        var label=textObj.GetComponent<TextMeshProUGUI>();
        label.font=canvas.GetComponentInChildren<TMP_Text>(true).font;label.text="[CLOSE SHAKER]";label.fontSize=26;label.alignment=TextAlignmentOptions.Center;
        label.color=new Color(.78f,.94f,.92f);label.raycastTarget=false;
        var context=c.gameObject.AddComponent<ShakerContextUI>();
        so=new SerializedObject(context);so.FindProperty("coordinator").objectReferenceValue=c;so.FindProperty("closeButton").objectReferenceValue=buttonObj.GetComponent<Button>();so.ApplyModifiedPropertiesWithoutUndo();
        buttonObj.SetActive(false);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        RunBatch();
    }
    public static void RunBatch()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"save Scene and leave Play Mode before validation");
        SessionState.SetBool(Key,true);Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;
    }
    static void Hook(){deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"timeout");
            if(!Application.isPlaying)return;
            Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();
            if(Time.frameCount<3)return;
            if(step==0)
            {
                InteractionCoordinatorIceCheck.Run();InteractionCoordinatorMeasurementCheck.Run();InteractionCoordinatorShakerCheck.Run();InteractionCoordinatorJiggerCheck.Run();step=1;
            }
            else if(step==1)
            {
                if(!Completed("T03.JiggerCheck"))return;InteractionCoordinatorTransferCheck.Run();step=2;
            }
            else if(step==2)
            {
                if(!Completed("T06.TransferCheck"))return;
                coordinator=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();shaker=UnityEngine.Object.FindAnyObjectByType<ShakerPreparation>();
                ui=UnityEngine.Object.FindAnyObjectByType<ShakerContextUI>();container=shaker.GetComponent<DrinkContainer>();
                lid=(Transform)new SerializedObject(shaker).FindProperty("lid").objectReferenceValue;closedPosition=lid.localPosition;
                ui.Refresh();Require(!ui.IsCloseVisible && !coordinator.TryCloseShaker(),"Rest has no Close");
                StartClose(false);step=3;
            }
            else if(step==3 || step==4)
            {
                if(coordinator.CurrentActionState==ActionState.Closing)return;
                ui.Refresh();Require(shaker.State==ShakerState.ReadyToShake && coordinator.CurrentActionState==ActionState.Stable,"completion callback");
                Require(!ui.IsCloseVisible && !coordinator.TryCloseShaker(),"Ready hides and rejects repeat");
                Require(Vector3.Distance(lid.localPosition,closedPosition)<.000001f,"lid closed pose");
                Require(JsonUtility.ToJson(container)==liquid && shaker.ProcessIce==ice,"Close preserves liquid and Ice");
                Require(coordinator.PrimaryHeld==primary && coordinator.SecondaryHeld==secondary,"Held preserved");
                if(step==3){coordinator.ResetPreparation();StartClose(true);step=4;}
                else
                {
                    coordinator.TryReturnHeld();coordinator.TryReturnHeld();coordinator.ResetPreparation();container.Clear();
                    Require(coordinator.TryAcquireShaker(shaker)&&coordinator.TryCloseShaker(),"reset cancellation setup");
                    coordinator.ResetPreparation();step=5;
                }
            }
            else
            {
                ui.Refresh();Require(shaker.State==ShakerState.Rest && coordinator.CurrentActionState==ActionState.Stable && !ui.IsCloseVisible,"reset cancels Close");
                Debug.Log("T08_AUTOMATED_PASS: button; Closing lock; no-Ice/empty hands; liquid/Ice/dual Held preserved; lid animation/callback; repeat rejection; Reset cancellation; T03-T07 regression PASS");Finish(0);
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static void StartClose(bool withIce)
    {
        Require(coordinator.TryAcquireShaker(shaker),"Preparing");
        Require(Vector3.Distance(lid.localPosition,closedPosition)>.01f,"Preparing visibly open");
        if(withIce)
        {
            var well=(Transform)new SerializedObject(coordinator).FindProperty("iceWell").objectReferenceValue;
            Require(coordinator.TryAddProcessIce(well.GetComponent<Collider>()),"Process Ice setup");
            var bottle=GameObject.Find("Bottle_Vodka").GetComponent<PourableIngredient>();
            var jigger=(DrinkContainer)new SerializedObject(coordinator).FindProperty("jigger").objectReferenceValue;
            jigger.AddIngredient(bottle.ingredientData,25,false);Require(jigger.TransferTo(container,25),"liquid setup");
            Require(coordinator.TryAcquireBottle(bottle)&&coordinator.TryAcquireJigger(jigger),"dual Held setup");
        }
        liquid=JsonUtility.ToJson(container);ice=shaker.ProcessIce;primary=coordinator.PrimaryHeld;secondary=coordinator.SecondaryHeld;
        coordinator.SetActionState(ActionState.AutoPour);ui.Refresh();Require(!ui.IsCloseVisible&&!coordinator.TryCloseShaker(),"Busy rejects Close");coordinator.SetActionState(ActionState.Stable);
        ui.Refresh();Require(ui.IsCloseVisible,"Preparing button visible");
        var button=(Button)new SerializedObject(ui).FindProperty("closeButton").objectReferenceValue;button.onClick.Invoke();
        Require(coordinator.CurrentActionState==ActionState.Closing && coordinator.OwnsHeldInput,"button starts Closing and locks old mouse input");
        Require(!coordinator.TryCloseShaker()&&!coordinator.TryReturnHeld()&&!coordinator.TryStartJiggerTransfer(shaker),"conflicting actions rejected");
        var terminal=UnityEngine.Object.FindAnyObjectByType<RecipeTerminalPrototype>();terminal.OpenBrowser();Require(!terminal.IsBrowserOpen,"Tab browser gate");
    }
    static bool Completed(string key){var s=SessionState.GetString(key,"RUNNING");if(s=="RUNNING")return false;Require(s.StartsWith("PASS"),s);return true;}
    static void Require(bool condition,string message){if(!condition)throw new Exception("T08: "+message);}
    static void Finish(int code)
    {
        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
        SessionState.SetString("T08.CloseCheck",code==0?"PASS":"FAIL");
        if(Application.isBatchMode)EditorApplication.Exit(code);
        else EditorApplication.isPlaying=false;
    }
}
#endif
