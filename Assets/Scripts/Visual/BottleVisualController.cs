using CupPrototype.DrinkSystem;
using UnityEngine;

namespace CupPrototype.Visual
{
    /// <summary>
    /// 根据材料数据为共用模型和材质的瓶子设置独立显示颜色。
    /// </summary>
    public class BottleVisualController : MonoBehaviour
    {
        // ===== Inspector 绑定参数 =====
        /// <summary>根对象上的可倒出材料组件；为空时会自动查找。</summary>
        public PourableIngredient pourableIngredient;

        /// <summary>标签 Renderer；为空时会从 VisualRoot/Label_Color 自动查找。</summary>
        public Renderer labelRenderer;

        /// <summary>可选的瓶盖 Renderer。</summary>
        public Renderer capRenderer;

        /// <summary>是否让瓶盖使用与标签相同的材料颜色。</summary>
        public bool colorCap = false;

        // 缓存 Shader 属性 ID，避免初始化时重复按字符串查找。
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>
        /// 自动补全引用，并在不实例化或修改共享材质的前提下应用颜色。
        /// </summary>
        private void Awake()
        {
            if (pourableIngredient == null)
            {
                pourableIngredient = GetComponent<PourableIngredient>();
            }

            if (labelRenderer == null)
            {
                Transform visualRoot = transform.Find("VisualRoot");
                Transform label = visualRoot != null ? visualRoot.Find("Label_Color") : null;
                labelRenderer = label != null ? label.GetComponent<Renderer>() : null;
            }

            bool missingReference = false;
            if (pourableIngredient == null)
            {
                Debug.LogWarning($"[BottleVisualController] {name} 根对象缺少 PourableIngredient，无法设置瓶身标签颜色。", this);
                missingReference = true;
            }
            else if (pourableIngredient.ingredientData == null)
            {
                Debug.LogWarning($"[BottleVisualController] {name} 的 PourableIngredient 未配置 IngredientData，无法设置瓶身标签颜色。", this);
                missingReference = true;
            }

            if (labelRenderer == null)
            {
                Debug.LogWarning($"[BottleVisualController] {name} 未配置 labelRenderer，且在 VisualRoot/Label_Color 未找到 Renderer。", this);
                missingReference = true;
            }

            if (colorCap && capRenderer == null)
            {
                Debug.LogWarning($"[BottleVisualController] {name} 已启用 colorCap，但未配置 capRenderer。", this);
                missingReference = true;
            }

            if (missingReference)
            {
                return;
            }

            Color color = pourableIngredient.ingredientData.displayColor;
            ApplyColor(labelRenderer, color);

            if (colorCap)
            {
                ApplyColor(capRenderer, color);
            }
        }

        /// <summary>
        /// 使用 MaterialPropertyBlock 设置单个 Renderer 的颜色，优先使用 _BaseColor 并兼容 _Color。
        /// </summary>
        private void ApplyColor(Renderer targetRenderer, Color color)
        {
            Material material = targetRenderer.sharedMaterial;
            if (material == null)
            {
                Debug.LogWarning($"[BottleVisualController] {targetRenderer.name} 没有材质，无法设置颜色。", targetRenderer);
                return;
            }

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);

            if (material.HasProperty(BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, color);
            }
            else if (material.HasProperty(ColorId))
            {
                propertyBlock.SetColor(ColorId, color);
            }
            else
            {
                Debug.LogWarning($"[BottleVisualController] {targetRenderer.name} 的 Shader 不包含 _BaseColor 或 _Color。", targetRenderer);
                return;
            }

            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
