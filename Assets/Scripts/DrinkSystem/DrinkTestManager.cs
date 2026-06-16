using CupPrototype.Interaction;
using CupPrototype.UI;
using UnityEngine;

namespace CupPrototype.DrinkSystem
{
    public class DrinkTestManager : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask clickableLayers = ~0;
        [SerializeField] private float raycastDistance = 1000f;
        [SerializeField] private TargetDrinkData targetDrink;
        [SerializeField] private DrinkDebugUI debugUI;

        private PourableIngredient selectedIngredient;
        private PourableIngredient currentSelectedIngredient;
        private DrinkContainer currentPourContainer;
        private bool isPouring;
        private bool promptedMissingIngredientThisPress;

        public static bool HasSelectedIngredient { get; private set; }

        private void Awake()
        {
            HasSelectedIngredient = false;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                TasteCurrentDrink();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                ScoreCurrentDrink();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ClearCurrentDrink();
            }

            if (targetCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                promptedMissingIngredientThisPress = false;
                HandleMouseDown();
            }

            if (Input.GetMouseButton(0))
            {
                HandlePourHold();
            }

            if (Input.GetMouseButtonUp(0))
            {
                StopPouring();
                promptedMissingIngredientThisPress = false;
            }
        }

        private void HandleMouseDown()
        {
            if (!TryGetObjectUnderMouse(out RaycastHit hit))
            {
                return;
            }

            PourableIngredient pourableIngredient = hit.collider.GetComponentInParent<PourableIngredient>();
            if (pourableIngredient != null)
            {
                SelectIngredient(pourableIngredient);
                return;
            }

            DrinkContainer drinkContainer = hit.collider.GetComponentInParent<DrinkContainer>();
            if (drinkContainer != null)
            {
                if (selectedIngredient == null)
                {
                    return;
                }

                BeginPouring(drinkContainer);
            }
        }

        private void SelectIngredient(PourableIngredient pourableIngredient)
        {
            if (currentSelectedIngredient == pourableIngredient)
            {
                ClearSelectedIngredient();
                return;
            }

            if (currentSelectedIngredient != null && currentSelectedIngredient != pourableIngredient)
            {
                SelectionHighlight previousHighlight = currentSelectedIngredient.GetComponent<SelectionHighlight>();
                if (previousHighlight != null)
                {
                    previousHighlight.SetHighlighted(false);
                }
            }

            selectedIngredient = pourableIngredient;
            currentSelectedIngredient = pourableIngredient;
            HasSelectedIngredient = true;

            SelectionHighlight currentHighlight = currentSelectedIngredient.GetComponent<SelectionHighlight>();
            if (currentHighlight != null)
            {
                currentHighlight.SetHighlighted(true);
            }

            string ingredientName = selectedIngredient.ingredientData != null
                ? selectedIngredient.ingredientData.ingredientName
                : "Ingredient data not assigned";

            Debug.Log($"Selected ingredient: {ingredientName}", selectedIngredient);

            if (debugUI != null)
            {
                debugUI.SetSelectedIngredient(ingredientName);
            }
        }

        private void HandlePourHold()
        {
            if (!TryGetDrinkContainerUnderMouse(out DrinkContainer drinkContainer))
            {
                StopPouring();
                return;
            }

            if (selectedIngredient == null)
            {
                return;
            }

            if (!drinkContainer.CanAdd(0.001f))
            {
                StopPouring();
                return;
            }

            if (currentPourContainer != drinkContainer || !isPouring)
            {
                BeginPouring(drinkContainer);
            }

            float amount = selectedIngredient.pourRatePerSecond * Time.deltaTime;
            drinkContainer.AddIngredient(selectedIngredient.ingredientData, amount, false);

            if (debugUI != null)
            {
                debugUI.SetVolume(drinkContainer.CurrentVolume, drinkContainer.MaxVolume);
            }
        }

        private bool TryGetObjectUnderMouse(out RaycastHit hit)
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, raycastDistance, clickableLayers);
        }

        private bool TryGetDrinkContainerUnderMouse(out DrinkContainer drinkContainer)
        {
            drinkContainer = null;

            if (!TryGetObjectUnderMouse(out RaycastHit hit))
            {
                return false;
            }

            drinkContainer = hit.collider.GetComponentInParent<DrinkContainer>();
            return drinkContainer != null;
        }

        private void BeginPouring(DrinkContainer drinkContainer)
        {
            if (isPouring && currentPourContainer == drinkContainer)
            {
                return;
            }

            currentPourContainer = drinkContainer;
            isPouring = true;

            string ingredientName = selectedIngredient != null && selectedIngredient.ingredientData != null
                ? selectedIngredient.ingredientData.ingredientName
                : "Ingredient data not assigned";

            Debug.Log($"Started pouring: {ingredientName}", drinkContainer);
        }

        private void StopPouring()
        {
            if (!isPouring)
            {
                currentPourContainer = null;
                return;
            }

            Debug.Log("Stopped pouring.", currentPourContainer);
            currentPourContainer = null;
            isPouring = false;
        }

        private void PromptSelectIngredientOnce(Object context)
        {
            if (promptedMissingIngredientThisPress)
            {
                return;
            }

            Debug.Log("Please select an ingredient first.", context);
            promptedMissingIngredientThisPress = true;
        }

        private void ClearSelectedIngredient()
        {
            if (currentSelectedIngredient != null)
            {
                SelectionHighlight currentHighlight = currentSelectedIngredient.GetComponent<SelectionHighlight>();
                if (currentHighlight != null)
                {
                    currentHighlight.SetHighlighted(false);
                }
            }

            selectedIngredient = null;
            currentSelectedIngredient = null;
            HasSelectedIngredient = false;
            StopPouring();

            Debug.Log("Selected ingredient cleared.", this);

            if (debugUI != null)
            {
                debugUI.SetSelectedIngredient("None");
            }
        }

        private void TasteCurrentDrink()
        {
            DrinkContainer drinkContainer = FindFirstObjectByType<DrinkContainer>();
            if (drinkContainer == null)
            {
                Debug.Log("No DrinkContainer found in the scene.", this);
                return;
            }

            FlavorProfile profile = drinkContainer.GetCurrentFlavorProfile();
            string feedback = TasteFeedbackSystem.GenerateFeedback(profile);

            Debug.Log($"Current flavor: {profile.ToDebugString()}\nTaste: {feedback}", drinkContainer);

            if (debugUI != null)
            {
                debugUI.SetTasteFeedback(feedback);
            }
        }

        private void ScoreCurrentDrink()
        {
            if (targetDrink == null)
            {
                Debug.Log("Please assign a Target Drink in DrinkTestManager.", this);
                return;
            }

            DrinkContainer drinkContainer = FindFirstObjectByType<DrinkContainer>();
            if (drinkContainer == null)
            {
                Debug.Log("No DrinkContainer found in the scene.", this);
                return;
            }

            DrinkScoreResult scoreResult = DrinkScoreSystem.ScoreDrink(drinkContainer, targetDrink);
            string missingIngredients = scoreResult.missingRequiredIngredients.Count > 0
                ? string.Join(", ", scoreResult.missingRequiredIngredients)
                : "None";

            Debug.Log(
                $"Target Drink: {targetDrink.drinkName}\n" +
                $"Total: {scoreResult.totalScore:0.0}/100\n" +
                $"Flavor: {scoreResult.flavorScore:0.0}/60\n" +
                $"Volume: {scoreResult.volumeScore:0.0}/20\n" +
                $"Ingredient: {scoreResult.ingredientScore:0.0}/20\n" +
                $"Missing: {missingIngredients}\n" +
                $"Feedback: {scoreResult.feedbackText}",
                drinkContainer);

            if (debugUI != null)
            {
                debugUI.SetScoreResult(scoreResult);
            }
        }

        private void ClearCurrentDrink()
        {
            DrinkContainer drinkContainer = FindFirstObjectByType<DrinkContainer>();
            if (drinkContainer == null)
            {
                Debug.Log("No DrinkContainer found in the scene.", this);
                return;
            }

            drinkContainer.Clear();
            Debug.Log("Cup cleared.", drinkContainer);

            if (debugUI != null)
            {
                debugUI.SetVolume(0f, drinkContainer.MaxVolume);
                debugUI.SetTasteFeedback("None");
                debugUI.SetScoreResult(null);
            }
        }
    }
}
