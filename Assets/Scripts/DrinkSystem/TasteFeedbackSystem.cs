using System.Collections.Generic;

namespace CupPrototype.DrinkSystem
{
    public static class TasteFeedbackSystem
    {
        public static string GenerateFeedback(FlavorProfile profile)
        {
            List<string> feedbackParts = new List<string>();

            if (profile.sourness >= 7f)
            {
                feedbackParts.Add("Sourness is prominent.");
            }

            if (profile.sweetness >= 7f)
            {
                feedbackParts.Add("Sweetness is prominent.");
            }

            if (profile.bitterness >= 7f)
            {
                feedbackParts.Add("Bitterness is prominent.");
            }

            if (profile.freshness >= 7f)
            {
                feedbackParts.Add("Freshness is strong.");
            }

            if (profile.body >= 7f)
            {
                feedbackParts.Add("Body is strong.");
            }

            if (profile.aroma >= 7f)
            {
                feedbackParts.Add("Aroma is noticeable.");
            }

            if (profile.sourness < 3f && profile.sweetness < 3f && profile.bitterness < 3f)
            {
                feedbackParts.Add("The drink tastes weak.");
            }

            if (profile.body < 3f)
            {
                feedbackParts.Add("The body feels thin.");
            }

            if (feedbackParts.Count == 0)
            {
                return "Overall balanced.";
            }

            return string.Join(" ", feedbackParts);
        }
    }
}
