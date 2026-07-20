using CupPrototype.DrinkSystem;
using System.Text;

namespace CupPrototype.Scoring
{
    // 把已有评分结果整理成玩家可读文本，不改变评分算法。
    public static class DrinkScoreFeedbackFormatter
    {
        public static string Format(DrinkScoreResult result)
        {
            if (result == null)
            {
                return "Score: None";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Score: {result.totalScore:0.0}/100");
            builder.AppendLine($"Volume: {FormatBand(result.volumeScore, 20f, "Good", "Needs adjustment")}");
            builder.AppendLine($"Flavor: {FormatBand(result.flavorScore, 60f, "Balanced", "Needs adjustment")}");
            builder.Append("Ingredients: ");
            builder.AppendLine(result.missingRequiredIngredients != null && result.missingRequiredIngredients.Count > 0
                ? $"Missing {string.Join(", ", result.missingRequiredIngredients)}"
                : "Complete");

            if (!string.IsNullOrWhiteSpace(result.preparationFeedback))
            {
                builder.AppendLine($"Preparation: {result.preparationFeedback}");
            }

            if (!string.IsNullOrWhiteSpace(result.feedbackText))
            {
                builder.Append($"Feedback: {result.feedbackText}");
            }

            return builder.ToString();
        }

        private static string FormatBand(float score, float maxScore, string good, string low)
        {
            return score >= maxScore * 0.8f ? good : low;
        }
    }
}
