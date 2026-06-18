using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 材料数据资产 =====
    // 每一种可倒入材料都对应一个 IngredientData，用于记录颜色和基础风味数值。
    [CreateAssetMenu(fileName = "NewIngredient", menuName = "Cup Prototype/Ingredient Data")]
    public class IngredientData : ScriptableObject
    {
        // ===== 基础显示信息 =====
        public string ingredientName;
        public Color displayColor = Color.white;

        // ===== 风味维度 =====
        // 数值目前用于加权平均、试味反馈和目标饮品评分。
        public float sourness;
        public float sweetness;
        public float bitterness;
        public float freshness;
        public float body;
        public float aroma;
    }
}
