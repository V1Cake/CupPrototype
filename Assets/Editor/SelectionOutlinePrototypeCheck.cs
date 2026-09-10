#if UNITY_EDITOR
using System;
using System.Linq;
using CupPrototype.Interaction;
using UnityEditor;
using UnityEngine;

public static class SelectionOutlinePrototypeCheck
{
    [MenuItem("Tools/CupPrototype/Validate Selection Outline (Play Mode)")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run in Play Mode.");
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/SelectionOutline.shader");
        if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Outline shader missing or invalid.");
        foreach(var name in new[]{"Bottle_Vodka","Jigger_Test","Shaker_Test","Cup_Test"})
        {
            var root=GameObject.Find(name);
            var h=root?root.GetComponent<SelectionHighlight>():null;
            if(!h)throw new Exception(name+" missing SelectionHighlight.");
            var transforms=root.GetComponentsInChildren<Transform>(true);
            var matrices=transforms.Select(t=>t.localToWorldMatrix).ToArray();
            var renderers=root.GetComponentsInChildren<Renderer>(true);
            var materials=renderers.Select(r=>r.sharedMaterials).ToArray();
            var materialJson=materials.Select(a=>a.Select(m=>m?EditorJsonUtility.ToJson(m):"null").ToArray()).ToArray();
            bool wasSelected=h.State==SelectionHighlight.VisualState.Selected;
            try
            {
                h.SetHighlighted(true);
                if(h.State==SelectionHighlight.VisualState.Idle||!SelectionHighlight.Active.Contains(h))
                    throw new Exception(name+" failed selected registration.");
                if(!h.VisualRenderers.Any(r=>r&&r.enabled&&r.gameObject.activeInHierarchy))
                    throw new Exception(name+" has no visible silhouette source.");
                h.SetHighlighted(false);
                for(int i=0;i<transforms.Length;i++)
                    if(transforms[i].localToWorldMatrix!=matrices[i])throw new Exception(name+" transform changed.");
                for(int i=0;i<renderers.Length;i++)
                {
                    if(!renderers[i].sharedMaterials.SequenceEqual(materials[i]))throw new Exception(name+" material reference changed.");
                    for(int j=0;j<materials[i].Length;j++)
                        if((materials[i][j]?EditorJsonUtility.ToJson(materials[i][j]):"null")!=materialJson[i][j])
                            throw new Exception(name+" material values changed.");
                }
                if(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="OutlineVisual"&&t.gameObject.activeInHierarchy))
                    throw new Exception(name+" still has active legacy glow.");
            }
            finally {h.SetHighlighted(wasSelected);}
        }
        Debug.Log("[P5-3] PASS: four tools, state/registry, visual sources, unchanged transforms/materials, no legacy glow.");
    }
}
#endif

