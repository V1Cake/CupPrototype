using CupPrototype.DrinkSystem;
using TMPro;
using UnityEngine;

namespace CupPrototype.UI
{
    public class DrinkDebugUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI selectedIngredientText;
        [SerializeField] private TextMeshProUGUI volumeText;
        [SerializeField] private TextMeshProUGUI tasteFeedbackText;
        [SerializeField] private TextMeshProUGUI scoreText;

        private void Awake()
        {
            ClearAll();
        }

        public void SetSelectedIngredient(string ingredientName)
        {
            if (selectedIngredientText == null)
            {
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(ingredientName)
                ? "None"
                : ingredientName;

            selectedIngredientText.text = $"Ingredient: {displayName}";
        }

        public void SetVolume(float currentVolume, float maxVolume)
        {
            if (volumeText == null)
            {
                return;
            }

            volumeText.text = $"Volume: {currentVolume:0.##} / {maxVolume:0.##}";
        }

        public void SetTasteFeedback(string feedback)
        {
            if (tasteFeedbackText == null)
            {
                return;
            }

            string displayFeedback = string.IsNullOrWhiteSpace(feedback)
                ? "None"
                : feedback;

            tasteFeedbackText.text = $"Taste: {displayFeedback}";
        }

        public void SetScoreResult(DrinkScoreResult result)
        {
            if (scoreText == null)
            {
                return;
            }

            if (result == null)
            {
                scoreText.text = "Score: None";
                return;
            }

            string missingIngredients = result.missingRequiredIngredients != null && result.missingRequiredIngredients.Count > 0
                ? string.Join(", ", result.missingRequiredIngredients)
                : "None";

            scoreText.text =
                $"Score: {result.totalScore:0.0}/100\n" +
                $"Flavor: {result.flavorScore:0.0}/60\n" +
                $"Volume: {result.volumeScore:0.0}/20\n" +
                $"Ingredient: {result.ingredientScore:0.0}/20\n" +
                $"Missing: {missingIngredients}\n" +
                $"Feedback: {result.feedbackText}";
        }

        public void ClearAll()
        {
            SetSelectedIngredient("None");
            SetVolume(0f, 0f);
            SetTasteFeedback("None");
            SetScoreResult(null);
        }
    }
}
