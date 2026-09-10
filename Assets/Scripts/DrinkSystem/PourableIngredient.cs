using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 可倒出材料组件 =====
    // 挂在材料瓶上，负责指定这个瓶子对应哪种 IngredientData。
    public class PourableIngredient : MonoBehaviour
    {
        // ===== 材料引用与倒入参数 =====
        public IngredientData ingredientData;
        public float defaultPourAmount = 20f;
        [Header("Measurement Flow Rate")]
        [Min(0f)]
        [Tooltip("Bottle → Jigger 流速，单位 ml/秒。数值越大倒得越快。0 表示不能开始 Measurement。与 Pour Tilt Feedback 的 Tilt Speed 无关。")]
        public float pourRatePerSecond = 30f;
    }
}
