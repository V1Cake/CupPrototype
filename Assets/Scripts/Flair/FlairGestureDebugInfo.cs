namespace CupPrototype.Flair
{
    // 最近一次手势识别和动作匹配结果，供 Console 和屏幕调试面板共用。
    public class FlairGestureDebugInfo
    {
        public bool hasResult;
        public string activeToolName;
        public string templateId;
        public FlairGestureType gestureType;
        public float distance;
        public string actionName;
        public string matchMode;
        public string failureReason;
    }
}
