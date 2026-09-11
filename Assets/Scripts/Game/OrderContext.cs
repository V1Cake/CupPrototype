using CupPrototype.DrinkSystem;

namespace CupPrototype.Game
{
    public sealed class OrderContext
    {
        public TargetDrinkData Recipe { get; }
        public FlavorProfile ExpectedFlavor => Recipe.targetFlavor;
        public string RecommendedGlass { get; }
        public bool? ProcessIceRequired { get; }
        public bool? ServeIceRequired { get; }
        public int AttemptCount { get; private set; } = 1;
        public bool RemakeOccurred => AttemptCount > 1;

        public OrderContext(TargetDrinkData recipe, string recommendedGlass = null,
            bool? processIceRequired = null, bool? serveIceRequired = null)
        {
            if (!recipe) throw new System.ArgumentNullException(nameof(recipe));
            Recipe = recipe;
            RecommendedGlass = recommendedGlass;
            ProcessIceRequired = processIceRequired;
            ServeIceRequired = serveIceRequired;
        }

        // 仅记录重做次数；实际重做流程由后续 Ticket 接入。
        public void RecordRemake() => AttemptCount++;
    }
}
