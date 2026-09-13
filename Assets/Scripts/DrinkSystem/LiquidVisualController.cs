using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 液体视觉控制器 =====
    // 只负责 Liquid_Visual 的显示、高度、位置和颜色，不保存饮品数据。
    public class LiquidVisualController : MonoBehaviour
    {
        // ===== Inspector 绑定参数 =====
        public Transform liquidVisual;
        public Renderer liquidRenderer;
        public float liquidMinScaleY = 0.02f;
        public float liquidMaxScaleY = 0.45f;
        public float liquidBottomOffset = 0.08f;
        public bool hideWhenEmpty = true;

        [Header("Cup Surface (leave disabled for existing tools)")]
        public bool useCupSurface;
        public float surfaceMinHeight;
        public float surfaceMaxHeight;
        public AnimationCurve surfaceWidthByFill = AnimationCurve.Linear(0, 1, 1, 1);
        public Color baseColor = new Color(.85f, .36f, .08f, 1);
        private MaterialPropertyBlock surfaceProperties;
        private Mesh cupMesh;
        private Vector3[] cupVertices;
        private const int CupSegments = 32, CupRings = 8;

        // ===== 运行时缓存 =====
        // 记录初始位置和缩放，用于保证液体底部固定、只向上升高。
        private Vector3 initialLocalPosition;
        private Vector3 initialLocalScale;
        private float liquidBottomLocalY;
        private bool initialized;

        // ===== 初始化绑定与基准数据 =====
        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (liquidVisual == null)
            {
                Debug.LogWarning("[LiquidVisualController] liquidVisual is null; using this transform as liquidVisual.", this);
                liquidVisual = transform;
            }

            if (liquidRenderer == null)
            {
                liquidRenderer = liquidVisual.GetComponent<Renderer>();
            }

            if (liquidVisual == null)
            {
                Debug.LogWarning("[LiquidVisualController] liquidVisual is null", this);
                initialized = true;
                return;
            }

            if (liquidRenderer == null)
            {
                Debug.LogWarning("[LiquidVisualController] liquidRenderer is null", this);
            }

            initialLocalPosition = liquidVisual.localPosition;
            initialLocalScale = liquidVisual.localScale;
            if (useCupSurface)
            {
                cupVertices = new Vector3[(CupRings + 1) * CupSegments + 2];
                var triangles = new System.Collections.Generic.List<int>();
                for (int ring = 0; ring < CupRings; ring++)
                    for (int i = 0; i < CupSegments; i++)
                    {
                        int a = ring * CupSegments + i, b = ring * CupSegments + (i + 1) % CupSegments;
                        triangles.AddRange(new[] { a, a + CupSegments, b, b, a + CupSegments, b + CupSegments });
                    }
                for (int i = 0; i < CupSegments; i++)
                {
                    int next = (i + 1) % CupSegments, top = CupRings * CupSegments;
                    triangles.AddRange(new[] { cupVertices.Length - 2, i, next, cupVertices.Length - 1, top + next, top + i });
                }
                cupMesh = new Mesh { name = "Cup Liquid Volume" };
                cupMesh.vertices = cupVertices; cupMesh.triangles = triangles.ToArray();
                liquidVisual.GetComponent<MeshFilter>().sharedMesh = cupMesh;
            }
            liquidBottomLocalY = initialLocalPosition.y - initialLocalScale.y + liquidBottomOffset;
            initialized = true;

            if (hideWhenEmpty)
            {
                liquidVisual.gameObject.SetActive(false);
            }
        }

        // ===== 根据容量刷新液体视觉 =====
        // currentVolume/maxVolume 控制高度，currentColor 控制材质颜色。
        public void UpdateVisual(float currentVolume, float maxVolume, Color currentColor)
        {
            if (!initialized)
            {
                Initialize();
            }

            if (liquidVisual == null || maxVolume <= 0f)
            {
                return;
            }

            float volumeRatio = Mathf.Clamp01(currentVolume / maxVolume);

            if (currentVolume <= 0f)
            {
                if (hideWhenEmpty)
                {
                    liquidVisual.gameObject.SetActive(false);
                    return;
                }

                SetLiquidTransform(liquidMinScaleY);
                return;
            }

            liquidVisual.gameObject.SetActive(true);

            if (useCupSurface)
            {
                var position = initialLocalPosition;
                position.y = surfaceMinHeight;
                liquidVisual.localPosition = position;
                liquidVisual.localScale = initialLocalScale;
                for (int ring = 0; ring <= CupRings; ring++)
                {
                    float fill = volumeRatio * ring / CupRings;
                    float radius = .5f * Mathf.Max(0, surfaceWidthByFill.Evaluate(fill));
                    float height = (surfaceMaxHeight - surfaceMinHeight) * fill / initialLocalScale.y;
                    for (int i = 0; i < CupSegments; i++)
                    {
                        float angle = i * Mathf.PI * 2 / CupSegments;
                        cupVertices[ring * CupSegments + i] = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                    }
                }
                cupVertices[cupVertices.Length - 2] = Vector3.zero;
                cupVertices[cupVertices.Length - 1] = Vector3.up * (surfaceMaxHeight - surfaceMinHeight) * volumeRatio / initialLocalScale.y;
                cupMesh.vertices = cupVertices; cupMesh.RecalculateNormals(); cupMesh.RecalculateBounds();
                if (liquidRenderer)
                {
                    surfaceProperties ??= new MaterialPropertyBlock();
                    liquidRenderer.GetPropertyBlock(surfaceProperties);
                    surfaceProperties.SetColor("_BaseColor", baseColor);
                    surfaceProperties.SetColor("_Color", baseColor);
                    liquidRenderer.SetPropertyBlock(surfaceProperties);
                }
                return;
            }

            float newScaleY = Mathf.Lerp(liquidMinScaleY, liquidMaxScaleY, volumeRatio);
            SetLiquidTransform(newScaleY);

            if (liquidRenderer != null)
            {
                liquidRenderer.material.color = currentColor;
            }
        }

        // ===== 清空视觉 =====
        // 不影响 DrinkContainer 数据，只负责把液体显示恢复为空杯状态。
        public void ClearVisual()
        {
            if (!initialized)
            {
                Initialize();
            }

            if (liquidVisual == null)
            {
                return;
            }

            if (hideWhenEmpty)
            {
                liquidVisual.gameObject.SetActive(false);
                return;
            }

            UpdateVisual(0f, 1f, Color.clear);
        }

        // ===== 内部工具：设置液体缩放和位置 =====
        private void SetLiquidTransform(float scaleY)
        {
            Vector3 scale = initialLocalScale;
            scale.y = scaleY;
            liquidVisual.localScale = scale;

            Vector3 position = initialLocalPosition;
            position.y = liquidBottomLocalY + scaleY;
            liquidVisual.localPosition = position;
        }

        private void OnDestroy()
        {
            if (cupMesh) Destroy(cupMesh);
        }
    }
}
