using System.Collections.Generic;

namespace CupPrototype.DrinkSystem
{
    // ===== 试味反馈系统 =====
    // 把风味数值转换成原型阶段可读的自然语言反馈。
    public static class TasteFeedbackSystem
    {
        public static string GenerateFeedback(FlavorProfile actual, FlavorProfile expected, float tolerance)
        {
            var differences = new[]
            {
                (actual.sourness - expected.sourness, "Too sour.", "Not sour enough."),
                (actual.sweetness - expected.sweetness, "Too sweet.", "Not sweet enough."),
                (actual.bitterness - expected.bitterness, "Too bitter.", "Not bitter enough."),
                (actual.freshness - expected.freshness, "Too fresh.", "Not fresh enough."),
                (actual.body - expected.body, "Body is too heavy.", "Body is too light."),
                (actual.aroma - expected.aroma, "Aroma is too strong.", "Aroma is too weak.")
            };
            float largest = UnityEngine.Mathf.Max(.01f, tolerance);
            string feedback = "Overall close to expected.";
            foreach (var difference in differences)
            {
                float magnitude = UnityEngine.Mathf.Abs(difference.Item1);
                if (magnitude <= largest) continue;
                largest = magnitude;
                feedback = difference.Item1 > 0 ? difference.Item2 : difference.Item3;
            }
            return feedback;
        }

        // ===== 根据风味生成文本 =====
        public static string GenerateFeedback(FlavorProfile profile)
        {
            List<string> feedbackParts = new List<string>();

            // ===== 高强度特征判断 =====
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

            // ===== 低强度/偏淡判断 =====
            if (profile.sourness < 3f && profile.sweetness < 3f && profile.bitterness < 3f)
            {
                feedbackParts.Add("The drink tastes weak.");
            }

            if (profile.body < 3f)
            {
                feedbackParts.Add("The body feels thin.");
            }

            // ===== 默认平衡反馈 =====
            if (feedbackParts.Count == 0)
            {
                return "Overall balanced.";
            }

            return string.Join(" ", feedbackParts);
        }
    }
}
