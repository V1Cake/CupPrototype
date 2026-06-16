using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    [CreateAssetMenu(fileName = "NewTargetDrink", menuName = "Cup Prototype/Target Drink Data")]
    public class TargetDrinkData : ScriptableObject
    {
        public string drinkName;
        public float targetVolume = 100f;
        public float volumeTolerance = 10f;
        public FlavorProfile targetFlavor;
        public float flavorTolerance = 2f;
        public List<IngredientData> requiredIngredients = new List<IngredientData>();
    }
}
