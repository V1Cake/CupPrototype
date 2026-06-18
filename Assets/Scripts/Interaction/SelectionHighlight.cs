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

        // ===== 生命周期：初始化外轮廓对象 =====
        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (outlineObject == null)
            {
                CreateOutlineObject();
            }

            if (outlineObject != null)
            {
                outlineObject.SetActive(false);
            }
        }

        // ===== 对外接口：开关高亮 =====
        public void SetHighlighted(bool highlighted)
        {
            if (outlineObject == null)
            {
                return;
            }

            outlineObject.SetActive(highlighted);
        }

        // ===== 自动创建伪外轮廓对象 =====
        private void CreateOutlineObject()
        {
            if (targetRenderer == null)
            {
                Debug.LogWarning("SelectionHighlight needs a Renderer to create an outline object.", this);
                return;
            }

            MeshFilter sourceMeshFilter = GetComponent<MeshFilter>();
            if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
            {
                Debug.LogWarning("SelectionHighlight needs a MeshFilter with a mesh to create an outline object.", this);
                return;
            }

            outlineObject = new GameObject("OutlineVisual");
            outlineObject.transform.SetParent(transform);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one * outlineScale;

            MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
            outlineMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = CreateOutlineMaterial();
        }

        // ===== 创建外轮廓材质 =====
        // 优先使用 URP/Lit，并尽量开启 emission，保证原型阶段容易看见。
        private Material CreateOutlineMaterial()
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
            else if (targetRenderer.sharedMaterial != null)
            {
                material = new Material(targetRenderer.sharedMaterial);
            }
            else
            {
                Debug.LogWarning("SelectionHighlight could not find a suitable shader for the outline material.", this);
                Shader fallbackShader = Shader.Find("Sprites/Default");
                material = fallbackShader != null
                    ? new Material(fallbackShader)
                    : new Material(targetRenderer.material);
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
