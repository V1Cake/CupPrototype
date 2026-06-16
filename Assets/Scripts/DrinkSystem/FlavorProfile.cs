using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    [System.Serializable]
    public struct FlavorProfile
    {
        public float sourness;
        public float sweetness;
        public float bitterness;
        public float freshness;
        public float body;
        public float aroma;

        public static FlavorProfile Empty()
        {
            return new FlavorProfile();
        }

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

        public string ToDebugString()
        {
            return $"Sourness={sourness:0.0}, Sweetness={sweetness:0.0}, Bitterness={bitterness:0.0}, Freshness={freshness:0.0}, Body={body:0.0}, Aroma={aroma:0.0}";
        }
    }
}
