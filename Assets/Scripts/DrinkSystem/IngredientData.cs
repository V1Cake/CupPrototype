using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    [CreateAssetMenu(fileName = "NewIngredient", menuName = "Cup Prototype/Ingredient Data")]
    public class IngredientData : ScriptableObject
    {
        public string ingredientName;
        public Color displayColor = Color.white;
        public float sourness;
        public float sweetness;
        public float bitterness;
        public float freshness;
        public float body;
        public float aroma;
    }
}
