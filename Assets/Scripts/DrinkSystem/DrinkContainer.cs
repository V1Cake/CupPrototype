using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    public class DrinkContainer : MonoBehaviour
    {
        [System.Serializable]
        public class IngredientEntry
        {
            public IngredientData ingredient;
            public float amount;
        }

        [SerializeField] private float maxVolume = 100f;
        [SerializeField] private float currentVolume;
        [SerializeField] private Color currentColor = Color.clear;
        [SerializeField] private Transform liquidVisual;
        [SerializeField] private Renderer liquidRenderer;
        [SerializeField] private float liquidMinHeight = 0.02f;
        [SerializeField] private float liquidBottomOffset = 0.1f;
        [SerializeField] private float liquidMaxScaleY = 0.75f;
        [SerializeField] private List<IngredientEntry> ingredients = new List<IngredientEntry>();

        private float liquidBottomLocalY;
        private Vector3 liquidInitialLocalScale = Vector3.one;
        private Vector3 liquidInitialLocalPosition;

        public float MaxVolume => maxVolume;
        public float CurrentVolume => currentVolume;
        public Color CurrentColor => currentColor;
        public IReadOnlyList<IngredientEntry> Ingredients => ingredients;

        private void Awake()
        {
            if (liquidVisual != null)
            {
                liquidInitialLocalScale = liquidVisual.localScale;
                liquidInitialLocalPosition = liquidVisual.localPosition;
                liquidBottomLocalY = liquidInitialLocalPosition.y - liquidInitialLocalScale.y + liquidBottomOffset;

                if (liquidRenderer == null)
                {
                    liquidRenderer = liquidVisual.GetComponent<Renderer>();
                }

                UpdateLiquidVisual();
            }
        }

        public bool CanAdd(float amount)
        {
            return amount > 0f && currentVolume < maxVolume;
        }

        public void AddIngredient(IngredientData ingredient, float amount, bool logResult = true)
        {
            if (ingredient == null)
            {
                if (logResult)
                {
                    Debug.LogWarning("Cannot add ingredient: IngredientData is null.", this);
                }

                return;
            }

            if (!CanAdd(amount))
            {
                if (logResult)
                {
                    Debug.Log($"Cup is full. Cannot add {ingredient.ingredientName}. Volume: {currentVolume:0.##}/{maxVolume:0.##}", this);
                }

                return;
            }

            float addAmount = Mathf.Min(amount, maxVolume - currentVolume);
            float previousVolume = currentVolume;
            currentVolume += addAmount;

            currentColor = previousVolume <= 0f
                ? ingredient.displayColor
                : Color.Lerp(currentColor, ingredient.displayColor, addAmount / currentVolume);

            AddOrUpdateIngredientEntry(ingredient, addAmount);
            UpdateLiquidVisual();

            if (logResult)
            {
                Debug.Log($"Added ingredient: {ingredient.ingredientName}, Amount: {addAmount:0.##}, Volume: {currentVolume:0.##}/{maxVolume:0.##}, Color: R={currentColor.r:0.00}, G={currentColor.g:0.00}, B={currentColor.b:0.00}, A={currentColor.a:0.00}", this);
            }
        }

        public void Clear()
        {
            currentVolume = 0f;
            currentColor = Color.clear;
            ingredients.Clear();
            UpdateLiquidVisual();
        }

        public FlavorProfile GetCurrentFlavorProfile()
        {
            if (currentVolume <= 0f)
            {
                return FlavorProfile.Empty();
            }

            FlavorProfile profile = FlavorProfile.Empty();
            float totalAmount = 0f;

            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == null || entry.amount <= 0f)
                {
                    continue;
                }

                profile.AddWeighted(entry.ingredient, entry.amount);
                totalAmount += entry.amount;
            }

            profile.Divide(totalAmount);
            return profile;
        }

        public bool ContainsIngredient(IngredientData ingredient)
        {
            if (ingredient == null)
            {
                return false;
            }

            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == ingredient && entry.amount > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<IngredientEntry> GetIngredientEntries()
        {
            return ingredients;
        }

        private void UpdateLiquidVisual()
        {
            if (liquidVisual == null)
            {
                return;
            }

            if (maxVolume <= 0f || currentVolume <= 0f)
            {
                liquidVisual.gameObject.SetActive(false);
                SetLiquidHeight(liquidMinHeight);
                return;
            }

            liquidVisual.gameObject.SetActive(true);

            float volumeRatio = Mathf.Clamp01(currentVolume / maxVolume);
            float height = Mathf.Lerp(liquidMinHeight, liquidMaxScaleY, volumeRatio);
            SetLiquidHeight(height);

            if (liquidRenderer != null)
            {
                liquidRenderer.material.color = currentColor;
            }
        }

        private void SetLiquidHeight(float height)
        {
            Vector3 scale = liquidInitialLocalScale;
            scale.y = height;
            liquidVisual.localScale = scale;

            Vector3 position = liquidInitialLocalPosition;
            position.y = liquidBottomLocalY + height;
            liquidVisual.localPosition = position;
        }

        private void AddOrUpdateIngredientEntry(IngredientData ingredient, float amount)
        {
            foreach (IngredientEntry entry in ingredients)
            {
                if (entry.ingredient == ingredient)
                {
                    entry.amount += amount;
                    return;
                }
            }

            ingredients.Add(new IngredientEntry
            {
                ingredient = ingredient,
                amount = amount
            });
        }
    }
}
