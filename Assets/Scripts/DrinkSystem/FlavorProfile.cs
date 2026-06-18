using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    // ===== 综合风味结构 =====
    // 用来表示一杯饮品混合后的风味平均值，属于纯数据结构。
    [System.Serializable]
    public struct FlavorProfile
    {
        // ===== 风味维度 =====
        public float sourness;
        public float sweetness;
        public float bitterness;
        public float freshness;
        public float body;
        public float aroma;

        // ===== 创建空风味 =====
        public static FlavorProfile Empty()
        {
            return new FlavorProfile();
        }

        // ===== 按加入量累加材料风味 =====
        // 这里先累加“风味值 * 数量”，最后再 Divide 得到平均值。
        public void AddWeighted(IngredientData ingredient, float amount)
        {
            if (ingredient == null || amount <= 0f)
            {
                return;
            }

            sourness += ingredient.sourness * amount;
            sweetness += ingredient.sweetness * amount;
            bitterness += ingredient.bitterness * amount;
            freshness += ingredient.freshness * amount;
            body += ingredient.body * amount;
            aroma += ingredient.aroma * amount;
        }

        // ===== 计算加权平均 =====
        public void Divide(float totalAmount)
        {
            if (totalAmount <= 0f)
            {
                return;
            }

            sourness /= totalAmount;
            sweetness /= totalAmount;
            bitterness /= totalAmount;
            freshness /= totalAmount;
            body /= totalAmount;
            aroma /= totalAmount;
        }

        // ===== 调试输出 =====
        public string ToDebugString()
        {
            return $"Sourness={sourness:0.0}, Sweetness={sweetness:0.0}, Bitterness={bitterness:0.0}, Freshness={freshness:0.0}, Body={body:0.0}, Aroma={aroma:0.0}";
        }
    }
}
