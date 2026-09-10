using System;
using System.Collections.Generic;
using CupPrototype.DrinkSystem;
using CupPrototype.Game;
using CupPrototype.Interaction;
using UnityEngine;

namespace CupPrototype.UI
{
    // Future player UI reads gameplay state here; this component never owns or mutates that state.
    public class GameplayUIBridge : MonoBehaviour
    {
        [SerializeField] private DrinkTestManager drinkTestManager;
        [SerializeField] private DemoRoundManager demoRoundManager;
        [SerializeField] private DrinkContainer jiggerContainer;
        [SerializeField] private DrinkContainer scoringContainer;

        private readonly List<DrinkContainer> observedContainers = new List<DrinkContainer>();
        private static readonly IReadOnlyList<DrinkContainer.IngredientEntry> EmptyContents =
            Array.Empty<DrinkContainer.IngredientEntry>();

        public event Action Changed;
        public event Action SelectionChanged;
        public event Action InteractionStateChanged;
        public event Action<DrinkContainer> ContainerVolumeChanged;
        public event Action RecipeChanged;
        public event Action TasteChanged;
        public event Action EvaluationChanged;

        public PourableIngredient SelectedIngredient => drinkTestManager != null
            ? drinkTestManager.SelectedIngredient
            : null;

        // This is the explicitly selected transfer source; ordinary drag clicks do not create a global selection.
        public DrinkContainer SelectedContainer => drinkTestManager != null
            ? drinkTestManager.SelectedSourceContainer
            : null;

        public InteractableObject SelectedTool => SelectedContainer != null
            ? SelectedContainer.GetComponent<InteractableObject>()
            : null;

        public string InteractionState => drinkTestManager != null
            ? drinkTestManager.InteractionState
            : "Not Available";

        public DrinkContainer CurrentContainer => drinkTestManager != null && drinkTestManager.ActiveContainer != null
            ? drinkTestManager.ActiveContainer
            : scoringContainer;

        public float CurrentVolume => CurrentContainer != null ? CurrentContainer.CurrentVolume : 0f;
        public float Capacity => CurrentContainer != null ? CurrentContainer.MaxVolume : 0f;
        public IReadOnlyList<DrinkContainer.IngredientEntry> Contents => CurrentContainer != null
            ? CurrentContainer.Ingredients
            : EmptyContents;

        public DrinkContainer JiggerContainer => jiggerContainer;
        public float JiggerCurrentVolume => jiggerContainer != null ? jiggerContainer.CurrentVolume : 0f;
        public float JiggerCapacity => jiggerContainer != null ? jiggerContainer.MaxVolume : 0f;

        public TargetDrinkData CurrentOrder => demoRoundManager != null && demoRoundManager.CurrentTarget != null
            ? demoRoundManager.CurrentTarget
            : drinkTestManager != null
                ? drinkTestManager.CurrentTarget
                : null;

        public FlavorProfile CurrentTaste => scoringContainer != null
            ? scoringContainer.GetCurrentFlavorProfile()
            : FlavorProfile.Empty();

        public FlavorProfile TargetTaste => CurrentOrder != null
            ? CurrentOrder.targetFlavor
            : FlavorProfile.Empty();

        public DrinkScoreResult Evaluation => drinkTestManager != null
            ? drinkTestManager.LastEvaluation
            : null;

        public bool HasEvaluation => Evaluation != null;
        public float ResultScore => Evaluation != null ? Evaluation.totalScore : 0f;
        public string ResultComment => Evaluation != null ? Evaluation.feedbackText : string.Empty;

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (drinkTestManager == null)
            {
                drinkTestManager = FindAnyObjectByType<DrinkTestManager>();
            }

            if (demoRoundManager == null)
            {
                demoRoundManager = FindAnyObjectByType<DemoRoundManager>();
            }

            observedContainers.Clear();
            DrinkContainer[] containers = FindObjectsByType<DrinkContainer>(FindObjectsSortMode.None);
            for (int i = 0; i < containers.Length; i++)
            {
                DrinkContainer container = containers[i];
                observedContainers.Add(container);

                if (jiggerContainer == null && container.containerType == DrinkContainer.ContainerType.Jigger)
                {
                    jiggerContainer = container;
                }

                if (scoringContainer == null && container.containerType == DrinkContainer.ContainerType.FinalGlass)
                {
                    scoringContainer = container;
                }
            }

            if (scoringContainer == null && drinkTestManager != null)
            {
                scoringContainer = drinkTestManager.scoringContainer;
            }
        }

        private void Subscribe()
        {
            if (drinkTestManager != null)
            {
                drinkTestManager.SelectionChanged += HandleSelectionChanged;
                drinkTestManager.InteractionStateChanged += HandleInteractionStateChanged;
                drinkTestManager.TargetChanged += HandleRecipeChanged;
                drinkTestManager.EvaluationChanged += HandleEvaluationChanged;
            }

            for (int i = 0; i < observedContainers.Count; i++)
            {
                observedContainers[i].StateChanged += HandleContainerChanged;
            }
        }

        private void Unsubscribe()
        {
            if (drinkTestManager != null)
            {
                drinkTestManager.SelectionChanged -= HandleSelectionChanged;
                drinkTestManager.InteractionStateChanged -= HandleInteractionStateChanged;
                drinkTestManager.TargetChanged -= HandleRecipeChanged;
                drinkTestManager.EvaluationChanged -= HandleEvaluationChanged;
            }

            for (int i = 0; i < observedContainers.Count; i++)
            {
                if (observedContainers[i] != null)
                {
                    observedContainers[i].StateChanged -= HandleContainerChanged;
                }
            }
        }

        private void HandleSelectionChanged()
        {
            SelectionChanged?.Invoke();
            Changed?.Invoke();
        }

        private void HandleInteractionStateChanged()
        {
            InteractionStateChanged?.Invoke();
            Changed?.Invoke();
        }

        private void HandleRecipeChanged(TargetDrinkData _)
        {
            RecipeChanged?.Invoke();
            Changed?.Invoke();
        }

        private void HandleEvaluationChanged(DrinkScoreResult _)
        {
            EvaluationChanged?.Invoke();
            Changed?.Invoke();
        }

        private void HandleContainerChanged(DrinkContainer container)
        {
            ContainerVolumeChanged?.Invoke(container);
            if (container == scoringContainer)
            {
                TasteChanged?.Invoke();
            }

            Changed?.Invoke();
        }

        [ContextMenu("Log UI Data Snapshot")]
        public void LogDataSnapshot()
        {
            Debug.Log(GetDebugSnapshot(), this);
        }

        public string GetDebugSnapshot()
        {
            string orderName = CurrentOrder != null ? CurrentOrder.drinkName : "Not Available Yet";
            string evaluationText = HasEvaluation ? ResultScore.ToString("0.0") : "Not Available Yet";
            return $"[GameplayUIBridge] Interaction={InteractionState}, SelectedIngredient={GetObjectName(SelectedIngredient)}, " +
                $"SelectedContainer={GetObjectName(SelectedContainer)}, CurrentContainer={GetObjectName(CurrentContainer)}, " +
                $"Volume={CurrentVolume:0.0}/{Capacity:0.0}, Jigger={JiggerCurrentVolume:0.0}/{JiggerCapacity:0.0}, " +
                $"Order={orderName}, Score={evaluationText}";
        }

        private static string GetObjectName(UnityEngine.Object value)
        {
            return value != null ? value.name : "Not Available Yet";
        }
    }
}
