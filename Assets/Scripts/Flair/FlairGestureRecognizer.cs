using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 玩家手势通过开发者模板匹配识别，不再把形状规则写死在代码里。
    public class FlairGestureRecognizer
    {
        // 模板匹配阈值，平均距离越小越像；后续可以按手势类型细分。
        private const float MatchThreshold = 0.35f;

        private readonly GestureTemplateLibrary templateLibrary;

        public FlairGestureRecognizer(GestureTemplateLibrary library)
        {
            templateLibrary = library;
        }

        // 返回模板对应的手势类型，动作播放仍然来自 FlairableTool.actions 动作簿。
        public FlairGestureType Recognize(IReadOnlyList<Vector2> points)
        {
            return RecognizeDetailed(points).gestureType;
        }

        public GestureMatchResult RecognizeDetailed(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 2)
            {
                return GestureMatchResult.None;
            }

            if (templateLibrary == null || templateLibrary.GetTemplates().Count == 0)
            {
                Debug.LogWarning("[FlairGestureRecognizer] No gesture templates. Please record a template first.");
                return GestureMatchResult.None;
            }

            List<Vector2> normalizedPoints = GestureTemplateUtility.NormalizePoints(points);
            if (normalizedPoints.Count == 0)
            {
                return GestureMatchResult.None;
            }

            GestureTemplateData bestTemplate = null;
            float bestDistance = float.MaxValue;
            IReadOnlyList<GestureTemplateData> templates = templateLibrary.GetTemplates();
            for (int i = 0; i < templates.Count; i++)
            {
                GestureTemplateData template = templates[i];
                if (template == null || template.normalizedPoints == null || template.normalizedPoints.Count == 0)
                {
                    continue;
                }

                float distance = GestureTemplateUtility.CalculateAverageDistance(normalizedPoints, template.normalizedPoints);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTemplate = template;
                }
            }

            if (bestTemplate != null && bestDistance <= MatchThreshold)
            {
                Debug.Log($"[FlairGestureRecognizer] Best Match: {bestTemplate.templateId}, Type={bestTemplate.gestureType}, Distance={bestDistance:0.00}");
                return new GestureMatchResult
                {
                    gestureType = bestTemplate.gestureType,
                    templateId = bestTemplate.templateId,
                    distance = bestDistance,
                    isMatched = true
                };
            }

            return GestureMatchResult.None;
        }
    }
}
