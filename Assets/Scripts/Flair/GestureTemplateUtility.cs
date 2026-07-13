using System.Collections.Generic;
using UnityEngine;

namespace CupPrototype.Flair
{
    // 模板匹配核心工具：标准化轨迹并计算模板距离。
    public static class GestureTemplateUtility
    {
        public static List<Vector2> NormalizePoints(IReadOnlyList<Vector2> points, int targetPointCount = 64)
        {
            List<Vector2> validPoints = RemoveInvalidPoints(points);
            if (validPoints.Count == 0 || targetPointCount <= 0)
            {
                return new List<Vector2>();
            }

            if (targetPointCount == 1)
            {
                return new List<Vector2> { Vector2.zero };
            }

            List<Vector2> resampled = Resample(validPoints, targetPointCount);
            CenterOnOrigin(resampled);
            ScaleToUnitBox(resampled);
            return resampled;
        }

        public static float CalculateAverageDistance(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b)
        {
            if (a == null || b == null || a.Count == 0 || a.Count != b.Count)
            {
                return float.MaxValue;
            }

            float total = 0f;
            for (int i = 0; i < a.Count; i++)
            {
                total += Vector2.Distance(a[i], b[i]);
            }

            return total / a.Count;
        }

        private static List<Vector2> RemoveInvalidPoints(IReadOnlyList<Vector2> points)
        {
            List<Vector2> validPoints = new List<Vector2>();
            if (points == null)
            {
                return validPoints;
            }

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];
                if (!float.IsNaN(point.x) && !float.IsNaN(point.y) &&
                    !float.IsInfinity(point.x) && !float.IsInfinity(point.y))
                {
                    validPoints.Add(point);
                }
            }

            return validPoints;
        }

        private static List<Vector2> Resample(IReadOnlyList<Vector2> points, int targetPointCount)
        {
            List<Vector2> result = new List<Vector2> { points[0] };
            float pathLength = CalculatePathLength(points);
            if (pathLength <= Mathf.Epsilon)
            {
                while (result.Count < targetPointCount)
                {
                    result.Add(points[0]);
                }

                return result;
            }

            float interval = pathLength / (targetPointCount - 1);
            float accumulated = 0f;
            Vector2 previous = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                Vector2 current = points[i];
                float segmentLength = Vector2.Distance(previous, current);

                while (accumulated + segmentLength >= interval && result.Count < targetPointCount)
                {
                    float t = (interval - accumulated) / segmentLength;
                    Vector2 newPoint = Vector2.Lerp(previous, current, t);
                    result.Add(newPoint);
                    previous = newPoint;
                    segmentLength = Vector2.Distance(previous, current);
                    accumulated = 0f;
                }

                accumulated += segmentLength;
                previous = current;
            }

            while (result.Count < targetPointCount)
            {
                result.Add(points[points.Count - 1]);
            }

            return result;
        }

        private static float CalculatePathLength(IReadOnlyList<Vector2> points)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector2.Distance(points[i - 1], points[i]);
            }

            return length;
        }

        private static void CenterOnOrigin(List<Vector2> points)
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < points.Count; i++)
            {
                center += points[i];
            }

            center /= points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                points[i] -= center;
            }
        }

        private static void ScaleToUnitBox(List<Vector2> points)
        {
            Vector2 min = points[0];
            Vector2 max = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            Vector2 size = max - min;
            float scale = Mathf.Max(size.x, size.y);
            if (scale <= Mathf.Epsilon)
            {
                return;
            }

            for (int i = 0; i < points.Count; i++)
            {
                points[i] /= scale;
            }
        }
    }
}
