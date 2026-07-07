using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 第一版花式手势识别器：用启发式规则识别 Circle，后续可以替换为更复杂算法。
    public class FlairGestureRecognizer
    {
        // Circle 识别阈值集中在这里，方便后续调参。
        private const int MinPointCount = 12;
        private const float MinPathLengthPixels = 150f;
        private const float MinBoundsSizePixels = 50f;
        private const float MinBoundsAspect = 0.5f;
        private const float MaxBoundsAspect = 2f;
        private const float MaxCloseDistanceRatio = 0.6f;
        private const float MinTotalAngleDegrees = 270f;

        // 根据屏幕轨迹点识别花式手势。
        public FlairGestureType Recognize(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < MinPointCount)
            {
                return FlairGestureType.None;
            }

            float pathLength = 0f;
            Vector2 min = points[0];
            Vector2 max = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                pathLength += Vector2.Distance(points[i - 1], points[i]);
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            Vector2 size = max - min;
            if (pathLength < MinPathLengthPixels ||
                size.x < MinBoundsSizePixels ||
                size.y < MinBoundsSizePixels)
            {
                return FlairGestureType.None;
            }

            float aspect = size.x / size.y;
            if (aspect < MinBoundsAspect || aspect > MaxBoundsAspect)
            {
                return FlairGestureType.None;
            }

            float maxSide = Mathf.Max(size.x, size.y);
            if (Vector2.Distance(points[0], points[points.Count - 1]) > maxSide * MaxCloseDistanceRatio)
            {
                return FlairGestureType.None;
            }

            Vector2 center = (min + max) * 0.5f;
            float totalAngle = 0f;
            float previousAngle = AngleFromCenter(points[0], center);

            for (int i = 1; i < points.Count; i++)
            {
                float angle = AngleFromCenter(points[i], center);
                totalAngle += Mathf.DeltaAngle(previousAngle, angle);
                previousAngle = angle;
            }

            return Mathf.Abs(totalAngle) >= MinTotalAngleDegrees
                ? FlairGestureType.Circle
                : FlairGestureType.None;
        }

        private static float AngleFromCenter(Vector2 point, Vector2 center)
        {
            Vector2 offset = point - center;
            return Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        }
    }
}
