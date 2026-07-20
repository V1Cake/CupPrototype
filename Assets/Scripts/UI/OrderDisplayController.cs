using CupPrototype.DrinkSystem;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace CupPrototype.UI
{
    // 显示当前目标饮品订单；未绑定 TMP 时只输出 Warning。
    public class OrderDisplayController : MonoBehaviour
    {
        public TargetDrinkData targetDrink;
        public TextMeshProUGUI orderText;
        public bool updateOnStart = true;

        private void Start()
        {
            if (updateOnStart)
            {
                ShowTargetDrink(targetDrink);
            }
        }

        public void ShowTargetDrink(TargetDrinkData target)
        {
            targetDrink = target;
            if (targetDrink == null)
            {
                Debug.LogWarning("[OrderDisplayController] targetDrink is missing.", this);
                Clear();
                return;
            }

            if (orderText == null)
            {
                Debug.LogWarning("[OrderDisplayController] orderText is missing.", this);
                return;
            }

            orderText.text = FormatTarget(targetDrink);
        }

        public void Clear()
        {
            if (orderText != null)
            {
                orderText.text = "Order: None";
            }
        }

        private static string FormatTarget(TargetDrinkData target)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Order: {Display(target.drinkName, target.name)}");
            builder.AppendLine($"Target Volume: {target.targetVolume:0.##} ml");
            builder.AppendLine($"Key Ingredients: {FormatIngredients(target.requiredIngredients)}");
            builder.AppendLine($"Flavor: {FormatFlavor(target.targetFlavor)}");
            builder.Append($"Preparation: {target.requiredPreparation}");
            return builder.ToString();
        }

        private static string FormatIngredients(List<IngredientData> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
            {
                return "None";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] != null)
                {
                    names.Add(Display(ingredients[i].ingredientName, ingredients[i].name));
                }
            }

            return names.Count > 0 ? string.Join(", ", names) : "None";
        }

        private static string FormatFlavor(FlavorProfile flavor)
        {
            return $"Sour {flavor.sourness:0.#} / Sweet {flavor.sweetness:0.#} / Bitter {flavor.bitterness:0.#} / Fresh {flavor.freshness:0.#} / Body {flavor.body:0.#} / Aroma {flavor.aroma:0.#}";
        }

        private static string Display(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
