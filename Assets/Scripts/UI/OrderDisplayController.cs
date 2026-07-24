using CupPrototype.DrinkSystem;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace CupPrototype.UI
{
    // 显示当前目标饮品订单；未绑定 TMP 时只输出 Warning。
    public class OrderDisplayController : MonoBehaviour
    {
        // 仅缓存当前显示引用；目标切换必须由 DemoRoundManager 同时同步 UI 和评分系统。
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
            TargetDrinkData displayedTarget = target;
            if (displayedTarget == null)
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

            // 禁止使用旧缓存拼接名称；订单全部字段都来自本次传入的 displayedTarget。
            orderText.text = FormatTarget(displayedTarget);
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
            builder.AppendLine($"Order: {GetTargetDisplayName(target)}");
            builder.AppendLine($"Target Volume: {target.targetVolume:0.##} ml");
            builder.AppendLine($"Volume Tolerance: ±{target.volumeTolerance:0.##} ml");
            builder.AppendLine($"Flavor Tolerance: ±{target.flavorTolerance:0.##}");
            builder.AppendLine($"Key Ingredients: {FormatIngredients(target.requiredIngredients)}");
            builder.AppendLine($"Flavor: {FormatFlavor(target.targetFlavor)}");
            builder.Append($"Preparation: {target.requiredPreparation}");
            return builder.ToString();
        }

        // drinkName 与资产名明显不一致时回退到资产名，避免错误资产数据让三个订单显示同名。
        public static string GetTargetDisplayName(TargetDrinkData target)
        {
            if (target == null)
            {
                return "None";
            }

            string assetName = Regex.Replace(target.name.Replace('_', ' '), "([a-z])([A-Z])", "$1 $2");
            if (string.IsNullOrWhiteSpace(target.drinkName))
            {
                return assetName;
            }

            string displayKey = Regex.Replace(target.drinkName, @"[\W_]", string.Empty).ToLowerInvariant();
            string assetKey = Regex.Replace(target.name, @"[\W_]", string.Empty).ToLowerInvariant();
            return displayKey == assetKey ? target.drinkName : assetName;
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
