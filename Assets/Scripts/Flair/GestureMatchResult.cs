namespace CupPrototype.Flair
{
    // 手势模板匹配结果：识别器只负责告诉动作簿匹配到了哪个模板。
    public struct GestureMatchResult
    {
        public FlairGestureType gestureType;
        public string templateId;
        public float distance;
        public bool isMatched;

        public static readonly GestureMatchResult None = new GestureMatchResult
        {
            gestureType = FlairGestureType.None,
            templateId = string.Empty,
            distance = float.MaxValue,
            isMatched = false
        };
    }
}
