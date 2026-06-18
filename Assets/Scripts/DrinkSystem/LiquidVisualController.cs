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
            Debug.Log($"[LiquidVisualController] UpdateVisual called. Volume={currentVolume}/{maxVolume}", this);

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
    }
}
