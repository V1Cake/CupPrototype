using CupPrototype.DrinkSystem;
using CupPrototype.Flair;
using CupPrototype.Interaction;
using CupPrototype.Game;
using CupPrototype.UI;
using UnityEngine;

namespace CupPrototype.Validation
{
    // 场景配置自检，只输出 Warning，不自动改对象或资源。
    public class DemoSetupValidator : MonoBehaviour
    {
        public bool validateOnStart = true;
        public bool includeInactiveObjects = true;

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateSceneSetup();
            }
        }

        public void ValidateSceneSetup()
        {
            ValidateFlairTools();
            ValidateContainers();
            ValidateIngredients();
            ValidateLiquidVisuals();
            ValidateTemplateLibraries();
            ValidateOrderDisplays();
            ValidateRoundManagers();
            ValidateSelectionHighlights();
        }

        private void ValidateFlairTools()
        {
            foreach (FlairableTool tool in FindSceneObjects<FlairableTool>())
            {
                if (tool.visualRoot == null)
                {
                    Warn($"{tool.name} has FlairableTool but visualRoot is missing.", tool);
                }
            }
        }

        private void ValidateContainers()
        {
            foreach (DrinkContainer container in FindSceneObjects<DrinkContainer>())
            {
                if (container.liquidVisualController == null && container.GetComponentInChildren<LiquidVisualController>(true) == null)
                {
                    Warn($"{container.name} has DrinkContainer but no LiquidVisualController found.", container);
                }
            }
        }

        private void ValidateIngredients()
        {
            foreach (PourableIngredient ingredient in FindSceneObjects<PourableIngredient>())
            {
                if (ingredient.ingredientData == null)
                {
                    Warn($"{ingredient.name} has PourableIngredient but ingredientData is missing.", ingredient);
                }
            }
        }

        private void ValidateLiquidVisuals()
        {
            foreach (LiquidVisualController liquid in FindSceneObjects<LiquidVisualController>())
            {
                if (liquid.liquidVisual == null)
                {
                    Warn($"{liquid.name} has no liquidVisual bound.", liquid);
                }
            }
        }

        private void ValidateTemplateLibraries()
        {
            foreach (GestureTemplateLibrary library in FindSceneObjects<GestureTemplateLibrary>())
            {
                if (library.templateAssets == null || library.templateAssets.Count == 0)
                {
                    Warn("GestureTemplateLibrary has no template assets.", library);
                }

                library.ValidateTemplates();
            }
        }

        private void ValidateOrderDisplays()
        {
            foreach (OrderDisplayController orderDisplay in FindSceneObjects<OrderDisplayController>())
            {
                if (orderDisplay.targetDrink == null)
                {
                    Warn($"{orderDisplay.name} has OrderDisplayController but targetDrink is missing.", orderDisplay);
                }
            }
        }

        private void ValidateRoundManagers()
        {
            foreach (DemoRoundManager roundManager in FindSceneObjects<DemoRoundManager>())
            {
                if (roundManager.currentTarget == null)
                {
                    Warn($"{roundManager.name} has DemoRoundManager but currentTarget is missing.", roundManager);
                }
            }
        }

        private void ValidateSelectionHighlights()
        {
            foreach (SelectionHighlight highlight in FindSceneObjects<SelectionHighlight>())
            {
                if (highlight.GetComponentInChildren<Renderer>(true) == null)
                {
                    Warn($"{highlight.name} has SelectionHighlight but no Renderer found.", highlight);
                }
            }
        }

        private T[] FindSceneObjects<T>() where T : Object
        {
            return FindObjectsByType<T>(
                includeInactiveObjects ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
        }

        private void Warn(string message, Object context)
        {
            Debug.LogWarning($"[DemoSetupValidator] {message}", context);
        }
    }
}
