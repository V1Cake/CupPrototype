using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 开发者手绘手势模板的数据记录；MVP 阶段只在运行时使用。
    [System.Serializable]
    public class GestureTemplateData
    {
        // 模板名称，例如 Circle_Dev_01、BottleLoop_01。
        public string templateId;

        // 该模板对应的手势类型，玩家匹配成功后仍走动作簿。
        public FlairGestureType gestureType;

        // 标准化后的轨迹点，用于和玩家轨迹做平均距离匹配。
        public List<Vector2> normalizedPoints = new List<Vector2>();
    }
}
