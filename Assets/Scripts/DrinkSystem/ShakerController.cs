using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 摇杯控制器 =====
    // 负责检测拖动 Shaker 时产生的位置变化，并把移动距离累积为 ShakeLevel。
    // 当前阶段只做状态验证：ShakeLevel 达标后把 DrinkContainer 标记为 Mixed。
    public class ShakerController : MonoBehaviour
    {
        // ===== Inspector 绑定参数 =====
        // container 指向同物体上的 DrinkContainer；为空时 Awake 会自动获取。
        public DrinkContainer container;
        // shakeLevel 是当前摇晃进度；可在 Inspector 中观察，也会通过 C 键调试输出显示。
        public float shakeLevel = 0f;
        // shakeRequired 是达到 Mixed 状态所需的摇晃进度；数值越高，需要拖动摇晃越久。
        public float shakeRequired = 100f;
        // shakeGainMultiplier 控制拖动距离转化为摇晃进度的倍率；数值越大，越容易摇匀。
        public float shakeGainMultiplier = 15f;
        // minMoveDistance 用于过滤非常小的抖动，避免静止时误增加 ShakeLevel。
        public float minMoveDistance = 0.02f;
        // requireMouseHold 为 true 时，只有按住鼠标左键拖动才会计入摇晃。
        public bool requireMouseHold = true;

        // ===== 内部状态 =====
        private Vector3 lastPosition;
        private bool initialized;

        // ===== 对外只读访问 =====
        public float ShakeLevel => shakeLevel;
        public float ShakeRequired => shakeRequired;

        // ===== 生命周期：初始化引用和起始位置 =====
        private void Awake()
        {
            Initialize();
        }

        // ===== 每帧检测摇晃 =====
        private void Update()
        {
            if (!initialized)
            {
                Initialize();
            }

            if (container == null)
            {
                return;
            }

            if (container.containerType != DrinkContainer.ContainerType.Shaker)
            {
                lastPosition = transform.position;
                return;
            }

            if (container.CurrentVolume <= 0f)
            {
                ResetShake();
                lastPosition = transform.position;
                return;
            }

            if (requireMouseHold && !Input.GetMouseButton(0))
            {
                lastPosition = transform.position;
                return;
            }

            // ===== 摇晃距离计算 =====
            // 只统计 X/Z 平面位移，避免未来模型动画、Y 轴校准或拖拽高度锁定影响摇晃判断。
            Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
            Vector2 lastXZ = new Vector2(lastPosition.x, lastPosition.z);
            float distance = Vector2.Distance(currentXZ, lastXZ);
            if (distance > minMoveDistance)
            {
                shakeLevel += distance * shakeGainMultiplier;
                shakeLevel = Mathf.Clamp(shakeLevel, 0f, shakeRequired);
                UpdateMixState();
            }

            lastPosition = transform.position;
        }

        // ===== 初始化 =====
        // 自动绑定同物体上的 DrinkContainer，并记录当前位置作为摇晃检测起点。
        private void Initialize()
        {
            if (container == null)
            {
                container = GetComponent<DrinkContainer>();
            }

            lastPosition = transform.position;
            initialized = true;
        }

        // ===== 重置摇晃进度 =====
        // DrinkContainer.Clear、容器变空、或新材料进入 Shaker 时调用，让摇杯回到未混合状态。
        public void ResetShake()
        {
            shakeLevel = 0f;
            lastPosition = transform.position;

            if (container != null)
            {
                container.mixState = DrinkContainer.MixState.Unmixed;
            }
        }

        // ===== 摇晃调试字符串 =====
        // DrinkContainer.GetDebugSummary 调用，用一行文本显示当前摇晃进度。
        public string GetShakeDebugString()
        {
            return $"ShakeLevel={shakeLevel:0.0}/{shakeRequired:0.0}";
        }

        // ===== 混合状态更新 =====
        // 根据 ShakeLevel 把容器标记为未混合、部分混合或已混合。
        private void UpdateMixState()
        {
            if (container == null)
            {
                return;
            }

            if (shakeLevel <= 0f)
            {
                container.mixState = DrinkContainer.MixState.Unmixed;
            }
            else if (shakeLevel < shakeRequired)
            {
                container.mixState = DrinkContainer.MixState.PartiallyMixed;
            }
            else
            {
                container.mixState = DrinkContainer.MixState.Mixed;
            }
        }
    }
}
