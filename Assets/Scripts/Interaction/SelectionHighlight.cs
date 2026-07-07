using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Interaction
{
    // ===== 选择高亮显示 =====
    // 挂在材料瓶上，用放大的 OutlineVisual 子物体模拟外轮廓高亮。
    public class SelectionHighlight : MonoBehaviour
    {
        // ===== Inspector 绑定参数 =====
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private GameObject outlineObject;
        [SerializeField] private Color outlineColor = Color.yellow;
        [SerializeField] private float outlineScale = 1.08f;

        private Renderer[] targetRenderers;
        private readonly List<GameObject> outlineObjects = new List<GameObject>();

        // ===== 生命周期：初始化外轮廓对象 =====
        private void Awake()
        {
            ResolveTargetRenderers();

            if (outlineObject != null)
            {
                outlineObjects.Add(outlineObject);
            }
            else
            {
                CreateOutlineObjects();
            }

            for (int i = 0; i < outlineObjects.Count; i++)
            {
                outlineObjects[i].SetActive(false);
            }
        }

        // ===== 对外接口：开关高亮 =====
        public void SetHighlighted(bool highlighted)
        {
            for (int i = 0; i < outlineObjects.Count; i++)
            {
                outlineObjects[i].SetActive(highlighted);
            }
        }

        private void ResolveTargetRenderers()
        {
            if (targetRenderer == null)
            {
                // VisualRoot 结构下 Renderer 可能在子物体，例如 VisualRoot/Bottle_Model。
                targetRenderers = GetComponentsInChildren<Renderer>(true);
                if (targetRenderers.Length == 0)
                {
                    Debug.LogWarning($"[SelectionHighlight] No Renderer found on {name} or its children.", this);
                }
            }
            else
            {
                targetRenderers = new[] { targetRenderer };
            }
        }

        // ===== 自动创建伪外轮廓对象 =====
        private void CreateOutlineObjects()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                MeshFilter sourceMeshFilter = renderer.GetComponent<MeshFilter>();
                if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
                {
                    Debug.LogWarning($"[SelectionHighlight] Renderer {renderer.name} needs a MeshFilter with a mesh to create an outline object.", this);
                    continue;
                }

                GameObject createdOutline = new GameObject("OutlineVisual");
                createdOutline.transform.SetParent(renderer.transform);
                createdOutline.transform.localPosition = Vector3.zero;
                createdOutline.transform.localRotation = Quaternion.identity;
                createdOutline.transform.localScale = Vector3.one * outlineScale;

                MeshFilter outlineMeshFilter = createdOutline.AddComponent<MeshFilter>();
                outlineMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

                MeshRenderer outlineRenderer = createdOutline.AddComponent<MeshRenderer>();
                outlineRenderer.sharedMaterial = CreateOutlineMaterial(renderer);

                outlineObjects.Add(createdOutline);
                if (outlineObject == null)
                {
                    outlineObject = createdOutline;
                }
            }
        }

        // ===== 创建外轮廓材质 =====
        // 优先使用 URP/Lit，并尽量开启 emission，保证原型阶段容易看见。
        private Material CreateOutlineMaterial(Renderer sourceRenderer)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("URP/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material;
            if (shader != null)
            {
                material = new Material(shader);
            }
            else if (sourceRenderer.sharedMaterial != null)
            {
                material = new Material(sourceRenderer.sharedMaterial);
            }
            else
            {
                Debug.LogWarning("SelectionHighlight could not find a suitable shader for the outline material.", this);
                Shader fallbackShader = Shader.Find("Sprites/Default");
                material = fallbackShader != null
                    ? new Material(fallbackShader)
                    : new Material(sourceRenderer.material);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", outlineColor);
            }
            else
            {
                material.color = outlineColor;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", outlineColor);
            }

            return material;
        }
    }
}
