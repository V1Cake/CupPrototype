using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 可提交到 Git 的手势模板资产；运行时会转成 GestureTemplateData 参与匹配。
    [CreateAssetMenu(menuName = "Cup Prototype/Gesture Template", fileName = "GestureTemplate")]
    public class GestureTemplateAsset : ScriptableObject
    {
        // 模板名称，例如 Circle_Dev_01。
        public string templateId;

        // 该模板对应的手势类型。
        public FlairGestureType gestureType;

        // 标准化后的开发者手绘轨迹点。
        public List<Vector2> normalizedPoints = new List<Vector2>();

        public GestureTemplateData ToRuntimeData()
        {
            return new GestureTemplateData
            {
                templateId = templateId,
                gestureType = gestureType,
                normalizedPoints = new List<Vector2>(normalizedPoints)
            };
        }
    }
}
