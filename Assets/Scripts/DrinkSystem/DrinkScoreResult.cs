using System.Collections.Generic;

namespace CupPrototype.DrinkSystem
{
    public class DrinkScoreResult
    {
        public float totalScore;
        public float flavorScore;
        public float volumeScore;
        public float ingredientScore;
        public List<string> missingRequiredIngredients = new List<string>();
        public string feedbackText;
    }
}
