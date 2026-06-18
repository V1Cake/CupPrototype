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
        public float pourRatePerSecond = 30f;
    }
}
