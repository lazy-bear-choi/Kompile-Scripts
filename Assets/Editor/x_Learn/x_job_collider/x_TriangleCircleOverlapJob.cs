using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public struct x_TriangleCircleOverlapJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float2> TriangleA;
    [ReadOnly] public NativeArray<float2> TriangleB;
    [ReadOnly] public NativeArray<float2> TriangleC;
    [ReadOnly] public NativeArray<float2> CircleCenters;
    [ReadOnly] public NativeArray<float> CircleRadii;

    public NativeArray<bool> OverlapResults;

    public void Execute(int index)
    {
        float2 a = TriangleA[index];
        float2 b = TriangleB[index];
        float2 c = TriangleC[index];
        float2 circleCenter = CircleCenters[index];
        float circleRadius = CircleRadii[index];
        float circleRadiusSq = circleRadius * circleRadius;

        // 1. AABB Check
        float minTriangleX = math.min(a.x, math.min(b.x, c.x));
        float maxTriangleX = math.max(a.x, math.max(b.x, c.x));
        float minTriangleY = math.min(a.y, math.min(b.y, c.y));
        float maxTriangleY = math.max(a.y, math.max(b.y, c.y));

        float minCircleX = circleCenter.x - circleRadius;
        float maxCircleX = circleCenter.x + circleRadius;
        float minCircleY = circleCenter.y - circleRadius;
        float maxCircleY = circleCenter.y + circleRadius;

        if (maxTriangleX < minCircleX || minTriangleX > maxCircleX ||
            maxTriangleY < minCircleY || minTriangleY > maxCircleY)
        {
            OverlapResults[index] = false;
            return;
        }

        // 2. Detailed Overlap Check if AABBs overlap
        if (IsPointInTriangle(circleCenter, a, b, c))
        {
            OverlapResults[index] = true;
            return;
        }

        if (IsPointInCircle(a, circleCenter, circleRadiusSq) ||
            IsPointInCircle(b, circleCenter, circleRadiusSq) ||
            IsPointInCircle(c, circleCenter, circleRadiusSq))
        {
            OverlapResults[index] = true;
            return;
        }

        if (IsLineCircleOverlap(a, b, circleCenter, circleRadiusSq) ||
            IsLineCircleOverlap(b, c, circleCenter, circleRadiusSq) ||
            IsLineCircleOverlap(c, a, circleCenter, circleRadiusSq))
        {
            OverlapResults[index] = true;
            return;
        }

        OverlapResults[index] = false;
    }

    private static bool IsPointInCircle(float2 point, float2 circleCenter, float circleRadiusSq)
    {
        return math.distancesq(point, circleCenter) <= circleRadiusSq;
    }

    private static bool IsPointInTriangle(float2 p, float2 a, float2 b, float2 c)
    {
        float s = (a.y * c.x - a.x * c.y) + (c.y - a.y) * b.x + (a.x - c.x) * b.y;
        if (s == 0) return false;
        float s1 = (a.y * c.x - a.x * c.y) + (c.y - a.y) * p.x + (a.x - c.x) * p.y;
        float s2 = (a.x - b.x) * p.y + (b.y - a.y) * p.x + a.x * b.y - b.x * a.y;
        float s_total = s;
        float alpha = s1 / s_total;
        float beta = s2 / s_total;
        float gamma = 1.0f - alpha - beta;
        return alpha >= 0 && beta >= 0 && gamma >= 0;
    }

    private static bool IsLineCircleOverlap(float2 p1, float2 p2, float2 center, float radiusSq)
    {
        float2 lineDir = p2 - p1;
        float lineLengthSquared = math.lengthsq(lineDir);
        if (lineLengthSquared == 0)
        {
            return IsPointInCircle(p1, center, radiusSq);
        }
        float t = math.dot(center - p1, lineDir) / lineLengthSquared;
        t = math.clamp(t, 0f, 1f);
        float2 closestPoint = p1 + t * lineDir;
        return math.distancesq(closestPoint, center) <= radiusSq;
    }
}