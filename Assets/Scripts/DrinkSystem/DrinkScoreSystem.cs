using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    public static class DrinkScoreSystem
    {
        private const float MaxFlavorScore = 60f;
        private const float MaxVolumeScore = 20f;
        private const float MaxIngredientScore = 20f;
        private const int FlavorDimensionCount = 6;

        public static DrinkScoreResult ScoreDrink(DrinkContainer container, TargetDrinkData target)
        {
            DrinkScoreResult result = new DrinkScoreResult();

            if (container == null || target == null)
            {
                result.feedbackText = "Cannot score: missing drink container or target drink data.";
                return result;
            }

            result.flavorScore = CalculateFlavorScore(container.GetCurrentFlavorProfile(), target);
            result.volumeScore = CalculateVolumeScore(container.CurrentVolume, target);
            result.ingredientScore = CalculateIngredientScore(container, target, result.missingRequiredIngredients);
            result.totalScore = result.flavorScore + result.volumeScore + result.ingredientScore;
            result.feedbackText = GenerateFeedback(container, target, result);

            return result;
        }

        private static float CalculateFlavorScore(FlavorProfile current, TargetDrinkData target)
        {
            float tolerance = Mathf.Max(0.01f, target.flavorTolerance);
            float maxPenaltyPerDimension = MaxFlavorScore / FlavorDimensionCount;
            float score = MaxFlavorScore;

            score -= GetFlavorPenalty(Mathf.Abs(current.sourness - target.targetFlavor.sourness), tolerance, maxPenaltyPerDimension);
            score -= GetFlavorPenalty(Mathf.Abs(current.sweetness - target.targetFlavor.sweetness), tolerance, maxPenaltyPerDimension);
            score -= GetFlavorPenalty(Mathf.Abs(current.bitterness - target.targetFlavor.bitterness), tolerance, maxPenaltyPerDimension);
            score -= GetFlavorPenalty(Mathf.Abs(current.freshness - target.targetFlavor.freshness), tolerance, maxPenaltyPerDimension);
            score -= GetFlavorPenalty(Mathf.Abs(current.body - target.targetFlavor.body), tolerance, maxPenaltyPerDimension);
            score -= GetFlavorPenalty(Mathf.Abs(current.aroma - target.targetFlavor.aroma), tolerance, maxPenaltyPerDimension);

            return Mathf.Clamp(score, 0f, MaxFlavorScore);
        }

        private static float GetFlavorPenalty(float difference, float tolerance, float maxPenalty)
        {
            if (difference <= tolerance)
            {
                return 0f;
            }

            return Mathf.Clamp((difference - tolerance) / tolerance * maxPenalty, 0f, maxPenalty);
        }

        private static float CalculateVolumeScore(float currentVolume, TargetDrinkData target)
        {
            float tolerance = Mathf.Max(0.01f, target.volumeTolerance);
            float difference = Mathf.Abs(currentVolume - target.targetVolume);

            if (difference <= tolerance)
            {
                return MaxVolumeScore;
            }

            float score = MaxVolumeScore - ((difference - tolerance) / tolerance * MaxVolumeScore);
            return Mathf.Clamp(score, 0f, MaxVolumeScore);
        }

        private static float CalculateIngredientScore(DrinkContainer container, TargetDrinkData target, List<string> missingRequiredIngredients)
        {
            if (target.requiredIngredients == null || target.requiredIngredients.Count == 0)
            {
                return MaxIngredientScore;
            }

            int requiredCount = 0;
            int missingCount = 0;

            foreach (IngredientData ingredient in target.requiredIngredients)
            {
                if (ingredient == null)
                {
                    continue;
                }

                requiredCount++;

                if (!container.ContainsIngredient(ingredient))
                {
                    missingCount++;
                    missingRequiredIngredients.Add(GetIngredientName(ingredient));
                }
            }

            if (requiredCount == 0)
            {
                return MaxIngredientScore;
            }

            float matchedRatio = (requiredCount - missingCount) / (float)requiredCount;
            return Mathf.Clamp(MaxIngredientScore * matchedRatio, 0f, MaxIngredientScore);
        }

        private static string GenerateFeedback(DrinkContainer container, TargetDrinkData target, DrinkScoreResult result)
        {
            List<string> feedbackParts = new List<string>();

            if (result.flavorScore >= MaxFlavorScore * 0.8f)
            {
                feedbackParts.Add("Flavor is close to target.");
            }
            else
            {
                feedbackParts.Add("Flavor needs adjustment.");
            }

            float volumeDifference = container.CurrentVolume - target.targetVolume;
            if (Mathf.Abs(volumeDifference) <= target.volumeTolerance)
            {
                feedbackParts.Add("Volume is close to target.");
            }
            else if (volumeDifference < 0f)
            {
                feedbackParts.Add("Volume is too low.");
            }
            else
            {
                feedbackParts.Add("Volume is too high.");
            }

            if (result.missingRequiredIngredients.Count > 0)
            {
                feedbackParts.Add($"Missing required ingredient: {string.Join(", ", result.missingRequiredIngredients)}");
            }

            if (result.totalScore >= 80f)
            {
                feedbackParts.Add("Overall result is good.");
            }
            else if (result.totalScore >= 60f)
            {
                feedbackParts.Add("Overall close to target.");
            }
            else
            {
                feedbackParts.Add("Overall result needs improvement.");
            }

            return string.Join(" ", feedbackParts);
        }

        private static string GetIngredientName(IngredientData ingredient)
        {
            if (!string.IsNullOrWhiteSpace(ingredient.ingredientName))
            {
                return ingredient.ingredientName;
            }

            return ingredient.name;
        }
    }
}
