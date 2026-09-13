#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using CupPrototype.DrinkSystem;
using CupPrototype.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CupLiquidVisualCheck
{
    const string Key = "T15.2.Running";
    static IEnumerator checks;
    static double deadline;
    static bool error;
    static IngredientData ingredient;
    static CupLiquidVisualCheck() { if (SessionState.GetBool(Key, false)) Hook(); }
    static DrinkContainer[] Cups()
    {
        var list = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>()).FindProperty("rackCups");
        return Enumerable.Range(0, list.arraySize).Select(i => (DrinkContainer)list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    }
    public static void SetupAndRun()
    {
        Require(!Application.isPlaying && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, "saved Edit Mode required");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        const string path = "Assets/Art/Glassware/Common/Materials/M_CupLiquid_Base.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Smoothness", .5f);
            AssetDatabase.CreateAsset(material, path);
        }
        foreach (var cup in Cups())
        {
            bool coupe = cup.name.Contains("Coupe"), highball = cup.name.Contains("Highball");
            var body = cup.GetComponentsInChildren<MeshFilter>().First(f => !coupe || f.name == "CoupeBowl");
            var points = body.sharedMesh.vertices.Select(v => cup.transform.InverseTransformPoint(body.transform.TransformPoint(v))).ToArray();
            var bounds = new Bounds(points[0], Vector3.zero); foreach(var v in points) bounds.Encapsulate(v);
            var visual = cup.transform.Find("Cup_Liquid_Surface");
            if (!visual)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
                UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.name = "Cup_Liquid_Surface"; visual.SetParent(cup.transform, false);
            }
            var controller = cup.GetComponent<LiquidVisualController>();
            if (!controller) controller = cup.gameObject.AddComponent<LiquidVisualController>();
            controller.useCupSurface = true; controller.hideWhenEmpty = true;
            controller.surfaceMinHeight = coupe ? .137f : highball ? .072f : .073f;
            controller.surfaceMaxHeight = coupe ? .178f : highball ? .192f : .147f;
            float diameter = coupe ? .0596f : highball ? .0485f : .0585f;
            visual.localPosition = new Vector3(bounds.center.x, controller.surfaceMinHeight, bounds.center.z);
            visual.localRotation = Quaternion.identity; visual.localScale = new Vector3(diameter, 1, diameter);
            controller.surfaceWidthByFill = coupe ? new AnimationCurve(
                new Keyframe(0, .006f/.0298f), new Keyframe((.147f-.137f)/.041f, .0192f/.0298f),
                new Keyframe((.163f-.137f)/.041f, .0271f/.0298f), new Keyframe(1, 1)) : AnimationCurve.Linear(0,1,1,1);
            for(int k=0;k<controller.surfaceWidthByFill.length;k++)
            {
                AnimationUtility.SetKeyLeftTangentMode(controller.surfaceWidthByFill,k,AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(controller.surfaceWidthByFill,k,AnimationUtility.TangentMode.Linear);
            }
            controller.liquidVisual = visual; controller.liquidRenderer = visual.GetComponent<Renderer>();
            controller.liquidRenderer.sharedMaterial = material;
            controller.liquidRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            controller.baseColor = new Color(.9f,.42f,.08f,1);
            cup.liquidVisualController = controller; visual.gameObject.SetActive(false);
            EditorUtility.SetDirty(cup); EditorUtility.SetDirty(controller);
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets(); RunBatch();
    }
    public static void RunBatch()
    {
        checks=null; error=false; SessionState.SetBool(Key,true); Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); EditorApplication.isPlaying=true;
    }
    static void Hook() { deadline=EditorApplication.timeSinceStartup+150; EditorApplication.update-=Tick; EditorApplication.update+=Tick; }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"timeout"); if(!Application.isPlaying)return;
            Application.runInBackground=true; EditorApplication.QueuePlayerLoopUpdate(); if(Time.frameCount<3)return;
            if(checks==null){Application.logMessageReceived+=Log;checks=RunChecks();}
            Require(!error,"runtime Error/Exception"); if(!checks.MoveNext())Finish(0);
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static IEnumerator RunChecks()
    {
        var c=UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        ingredient=ScriptableObject.CreateInstance<IngredientData>(); ingredient.displayColor=Color.magenta;
        var cups=Cups(); Require(cups.Length==3,"three Rack Cups");
        foreach(var cup in cups)
        {
            var v=cup.liquidVisualController;
            Require(v && v.useCupSurface && v.liquidVisual && v.liquidRenderer && v.liquidRenderer.sharedMaterial,"valid common visual configuration");
            Require(!v.liquidVisual.gameObject.activeSelf,"initial empty hidden");
            Require(c.TryAcquireCup(cup),"Cup acquisition unchanged");
            foreach(float ratio in new[]{.5f,1f})
            {
                cup.AddIngredient(ingredient,cup.MaxVolume*.5f,false); yield return null;
                Check(cup);
                var block=new MaterialPropertyBlock(); v.liquidRenderer.GetPropertyBlock(block);
                Require(block.GetColor("_BaseColor")==v.baseColor,"configured color independent of Ingredient color");
                Capture(cup.name+"-"+ratio);
            }
            typeof(DrinkTestManager).GetMethod("ClearCurrentDrink",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c.GetComponent<DrinkTestManager>(),null);
            Require(cup.IsEmpty() && !v.liquidVisual.gameObject.activeSelf,"R resets actual liquid and visual");
        }
        Debug.Log("T15_2_AUTOMATED_PASS: three configured cups; empty/half/full mapping; base color; actual data; R clears visuals");
    }
    public static void Check(DrinkContainer cup)
    {
        var v=cup.liquidVisualController; Require(v && v.useCupSurface,"cup visual exists");
        float ratio=Mathf.Clamp01(cup.CurrentVolume/cup.MaxVolume);
        Require(v.liquidVisual.gameObject.activeSelf==(cup.CurrentVolume>0),"visibility tracks actual data");
        if(ratio>0)
        {
            float top=v.liquidVisual.localPosition.y+v.liquidVisual.GetComponent<MeshFilter>().sharedMesh.bounds.max.y*v.liquidVisual.localScale.y;
            Require(Mathf.Abs(top-Mathf.Lerp(v.surfaceMinHeight,v.surfaceMaxHeight,ratio))<.00001f,"actual volume maps to configured height");
        }
    }
    static void Capture(string name)
    {
        var camera=Camera.main; var previous=camera.targetTexture; var active=RenderTexture.active;
        var rt=new RenderTexture(1280,720,24); var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"T15-2-"+name+".png"),tex.EncodeToPNG());
        }
        finally{camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception("T15.2: "+message);}
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)error=true;}
    static void Finish(int code)
    {
        Application.logMessageReceived-=Log;EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
        if(ingredient)UnityEngine.Object.DestroyImmediate(ingredient);
        if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.isPlaying=false;
    }
    public static void Audit()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var c = UnityEngine.Object.FindAnyObjectByType<InteractionCoordinator>();
        var list = new SerializedObject(c).FindProperty("rackCups");
        for (int i = 0; i < list.arraySize; i++)
        {
            var cup = (DrinkContainer)list.GetArrayElementAtIndex(i).objectReferenceValue;
            Debug.Log("CUP_AUDIT " + cup.name + " root=" + cup.transform.position + " scale=" + cup.transform.lossyScale);
            foreach (var mf in cup.GetComponentsInChildren<MeshFilter>(true))
            {
                var vertices = mf.sharedMesh.vertices.Select(v => cup.transform.InverseTransformPoint(mf.transform.TransformPoint(v))).ToArray();
                var bounds = new Bounds(vertices[0], Vector3.zero); foreach (var v in vertices) bounds.Encapsulate(v);
                var center = bounds.center;
                Debug.Log("CUP_MESH " + mf.name + " bounds=" + bounds + " material=" + mf.GetComponent<Renderer>().sharedMaterial.name);
                foreach (var group in vertices.GroupBy(v => Math.Round(v.y, 4)).OrderBy(g => g.Key))
                    Debug.Log("CUP_RING " + cup.name + " y=" + group.Key + " r=" + group.Min(v => new Vector2(v.x-center.x,v.z-center.z).magnitude) + "," + group.Max(v => new Vector2(v.x-center.x,v.z-center.z).magnitude));
            }
        }
        EditorApplication.Exit(0);
    }
}
#endif
