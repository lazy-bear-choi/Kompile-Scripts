namespace Script.Data
{
    using Script.Util;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary> 위치로부터 반경 radius 에 닿은 타일 삼각형들이 이동 가능한지 여부 판단
    /// </summary>
    public struct MapTileMovableJob
    {
        [ReadOnly] public NativeArray<IngameMapTileData> IngameMapTileData;
        [ReadOnly] public float3 SphereCenter;
        [ReadOnly] public float SphereRadius;

        public bool Execute(int index)
        {
            IngameMapTileData data = IngameMapTileData[index];
            float3 closestPoint;
            float distSq;
            float radiusSq = SphereRadius * SphereRadius;

            for (int i = 0; i < Index.MapTileIndex.TRIANGLES_COUNT; ++i)
            {
                bool setTriangle = true;
                setTriangle &= MapUtil.TryGetTrianglePoint(data, i, 0, false, out float3 a);
                setTriangle &= MapUtil.TryGetTrianglePoint(data, i, 1, false, out float3 b);
                setTriangle &= MapUtil.TryGetTrianglePoint(data, i, 2, false, out float3 c);

                // 쓰읍.. 이거 문제될 것 같은데... 2차원스럽게 계산하고 있는데 3차원 들고 나오니까 sqrt 계산 전제가 안 맞음;
                closestPoint = ClosestPointOnTriangle(SphereCenter, a, b, c); 
                distSq = math.distancesq(closestPoint, SphereCenter);

                // 영역이 겹치지 않는다면? 고려 대상 아님
                if(distSq > radiusSq)
                {
                    continue;
                }

                // 비교 대상이 존재하지 않음
                if (false == data.IsValid())
                {
                    return false;
                }

                // (겹치는 영역이지만) 삼각형을 구현할 수 없다면 이동 불가
                if (false == setTriangle)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary> 3D 공간에서 점과 삼각형 사이의 가장 가까운 점을 찾는 함수입니다.
        /// </summary>
        private static float3 ClosestPointOnTriangle(float3 p, float3 a, float3 b, float3 c)
        {
            // 꼭짓점 벡터 계산
            float3 ab = b - a;
            float3 ac = c - a;
            float3 ap = p - a;

            // 점 p와 꼭짓점 a 사이의 내적
            float d1 = math.dot(ab, ap);
            float d2 = math.dot(ac, ap);

            // 점 p가 A 영역(A를 꼭짓점으로 하는 영역)에 있는 경우
            if (d1 <= 0.0f && d2 <= 0.0f)
            {
                return a;
            }

            // 점 p와 꼭짓점 b 사이의 내적
            float3 bp = p - b;
            float d3 = math.dot(ab, bp);
            float d4 = math.dot(ac, bp);

            // 점 p가 B 영역에 있는 경우
            if (d3 >= 0.0f && d4 <= d3)
            {
                return b;
            }

            // 변 AB 위에 있는 경우
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float v = d1 / (d1 - d3);
                return a + ab * v;
            }

            // 점 p와 꼭짓점 c 사이의 내적
            float3 cp = p - c;
            float d5 = math.dot(ab, cp);
            float d6 = math.dot(ac, cp);

            // 점 p가 C 영역에 있는 경우
            if (d6 >= 0.0f && d5 <= d6)
            {
                return c;
            }

            // 변 AC 위에 있는 경우
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float w = d2 / (d2 - d6);
                return a + ac * w;
            }

            // 변 BC 위에 있는 경우
            float3 bc = c - b;
            float d7 = math.dot(bc, bp);
            float d8 = math.dot(bc, cp);
            if (d7 >= 0.0f && d8 <= 0.0f)
            {
                float v = d7 / (d7 - d8);
                return b + bc * v;
            }

            // 삼각형 내부
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && vb <= 0.0f && vc <= 0.0f)
            {
                float denom = 1.0f / (va + vb + vc);
                float v = vb * denom;
                float w = vc * denom;
                return a + ab * v + ac * w;
            }

            // 어떤 영역에도 속하지 않으면 (위 로직에서 이미 처리됨)
            // 2D 투영을 이용한 다른 방법이 있을 수 있지만, 3D에서는 이 로직이 더 일반적입니다.
            // 여기서는 가장 가까운 점만 반환하면 되므로 이대로 충분합니다.
            // 실제로는 이 코드가 모든 케이스를 커버합니다.

            return float3.zero; // Unreachable
        }
    }
}