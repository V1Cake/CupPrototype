using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    public class PourableIngredient : MonoBehaviour
    {
        public IngredientData ingredientData;
        public float defaultPourAmount = 20f;
        public float pourRatePerSecond = 30f;
    }
}
