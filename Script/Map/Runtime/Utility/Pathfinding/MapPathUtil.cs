namespace Script.Map.Runtime
{
    using Unity.Burst;
    using Unity.Mathematics;
    using Unity.VisualScripting.YamlDotNet.Serialization.NodeTypeResolvers;
    using UnityEngine;

    /// <summary>
    /// 상태 값이 없는 순수 수학 유틸리티 클래스. 좌표 변환 및 기하학적 판정을 담당
    /// </summary>
    public static class MapPathUtil
    {
        // --- [Map Spatial Partitioning & Bitwise Constants] ---
        /// <summary>
        /// 맵을 구역(grid) 단위로 로드/언로드 하기 위한 기준 크기<br/>
        /// 1개의 그리드는 64*64개의 타일로 구성
        /// </summary>
        public const int GRID_SIZE = 64;

        /// <summary>
        /// 1개의 타일 내부를 몇 개의 세부 칸(sub)으로 나눌지 결정하는 해상도<br/>
        /// 높이 연산 등에서 1유닛을 8등분(0.125f)하여 정밀하게 계산하는 데 사용
        /// </summary>
        public const int SIZE_TILE = 8;

        /// <summary>
        /// 그리드 내 로컬 타일 좌표(0~63)를 메모리에 압축할 때 사용하는 비트(bit) 개수
        /// </summary>
        public const int TILE_BITS = 6;

        /// <summary>
        /// CPU가 힘들어하는 나머지 연산(%)을 초고속 비트 연산(&)으로 대체하기 위한 마스크
        /// </summary>
        public const int TILE_MASK = (1 << TILE_BITS) - 1;

        /// <summary> 서브 타일을 구성하는 3개의 정점 모음 </summary>
        public static readonly int[] SubTileVertexMap = new int[]
        {
        0, 1, 3,    // s0
        1, 3, 6,    // s1
        3, 5, 6,    // s2
        0, 3, 5,    // s3
        1, 2, 4,    // s4
        2, 4, 7,    // s5
        4, 6, 7,    // s6
        1, 4, 6,    // s7
        5, 6, 8,    // s8
        6, 8, 11,   // s9
        8, 10, 11,  // s10
        5, 8, 10,   // s11
        6, 7, 9,    // s12
        7, 9, 12,   // s13
        9, 11, 12,  // s14
        6, 9, 11    // s15            
        };

        /// <summary> 그림의 v00 ~ v12 위치를 2D 좌표로 매핑 </summary>
        public static readonly float2[] VertexPositions = new float2[]
        {
        new float2(0.00f, 0.00f), // v00
        new float2(0.50f, 0.00f), // v01
        new float2(1.00f, 0.00f), // v02
        new float2(0.25f, 0.25f), // v03
        new float2(0.75f, 0.25f), // v04
        new float2(0.00f, 0.50f), // v05
        new float2(0.50f, 0.50f), // v06
        new float2(1.00f, 0.50f), // v07
        new float2(0.25f, 0.75f), // v08
        new float2(0.75f, 0.75f), // v09
        new float2(0.00f, 1.00f), // v10
        new float2(0.50f, 1.00f), // v11
        new float2(1.00f, 1.00f)  // v12
        };

        public static long ComputeTileIDInt(int3 pInt)
        {
            int absTx = pInt.x >> 3;
            int absTy = pInt.y >> 3;
            int absTz = pInt.z >> 3;

            int gX = absTx >> 6;
            int gY = absTy >> 6;
            int gZ = absTz >> 6;

            int tX = absTx & TILE_MASK;
            int tY = absTy & TILE_MASK;
            int tZ = absTz & TILE_MASK;

            uint tKey =
                (uint)((tX & TILE_MASK) << (TILE_BITS * 2)) |
                (uint)((tY & TILE_MASK) << (TILE_BITS * 1)) |
                (uint)((tZ & TILE_MASK) << (TILE_BITS * 0));

            byte bX = (byte)(sbyte)gX;
            byte bY = (byte)(sbyte)gY;
            byte bZ = (byte)(sbyte)gZ;

            uint gKey = (uint)((bX << 16) | (bY << 8) | bZ);

            return (((long)gKey) << 32) | tKey;
        }

        public static long ComputeID(int gKey, int tKey)
        { 
            const int SHIFT = 32;
            return ((long)gKey << SHIFT) | (uint)tKey;
        }

        public static Vector3 ComputeWorldPosition(long id)
        {
            int3 absPos = ComputeWorldPositionInt(id);
            return new Vector3(absPos.x, absPos.y, absPos.z);
        }

        public static int3 ComputeWorldPositionInt(long id)
        {
            uint gKey = (uint)((ulong)id >> 32);
            uint tKey = (uint)id;

            int gx = (sbyte)(byte)((gKey >> 16) & 0xFF);
            int gy = (sbyte)(byte)((gKey >> 8) & 0xFF);
            int gz = (sbyte)(byte)((gKey >> 0) & 0xFF);

            int tx = (int)((tKey >> (TILE_BITS * 2)) & TILE_MASK);
            int ty = (int)((tKey >> (TILE_BITS * 1)) & TILE_MASK);
            int tz = (int)((tKey >> (TILE_BITS * 0)) & TILE_MASK);

            return new int3(
                gx * GRID_SIZE + tx,
                gy * GRID_SIZE + ty,
                gz * GRID_SIZE + tz);
        }

        public static void ComputeKey(float3 worldPos, out int outGKey, out int outTKey)
        {
            int absTx = (int)math.floor(worldPos.x);
            int absTy = (int)math.floor(worldPos.y);
            int absTz = (int)math.floor(worldPos.z);

            int gX = absTx >> 6;
            int gY = absTy >> 6;
            int gZ = absTz >> 6;

            int tX = absTx & TILE_MASK;
            int tY = absTy & TILE_MASK;
            int tZ = absTz & TILE_MASK;

            outTKey = (tX << (TILE_BITS * 2))
                    | (tY << (TILE_BITS * 1))
                    | (tZ << (TILE_BITS * 0));

            byte bX = (byte)(sbyte)gX;
            byte bY = (byte)(sbyte)gY;
            byte bZ = (byte)(sbyte)gZ;

            outGKey = ((bX << 16) | (bY << 8) | bZ);
        }

        public static int ComputeGridKey(Vector3 worldPos)
        {
            int gx = Mathf.FloorToInt(worldPos.x / GRID_SIZE);
            int gy = Mathf.FloorToInt(worldPos.y / GRID_SIZE);
            int gz = Mathf.FloorToInt(worldPos.z / GRID_SIZE);

            byte bX = (byte)(sbyte)gx;
            byte bY = (byte)(sbyte)gy;
            byte bZ = (byte)(sbyte)gz;

            return (bX << 16) | (bY << 8) | bZ;
        }

        public static int ComputeGridKey(int gridKey, int3 offset)
        {
            Vector3Int target = GetGridPivot(gridKey) + new Vector3Int(offset.x, offset.y, offset.z);

            byte bX = (byte)(sbyte)target.x;
            byte bY = (byte)(sbyte)target.y;
            byte bZ = (byte)(sbyte)target.z;

            return (bX << 16) | (bY << 8) | bZ;
        }

        public static Vector3Int GetGridPivot(int gridKey)
        {
            int x = (sbyte)((gridKey >> 16) & 0xFF);
            int y = (sbyte)((gridKey >> 8) & 0xFF);
            int z = (sbyte)((gridKey >> 0) & 0xFF);

            return new Vector3Int(x, y, z);
        }

        [BurstCompile]
        public static bool IsSubTileValid(long naviMask, int sIndex0to15)
        {
            if (0 > sIndex0to15 || 15 < sIndex0to15)
            {
                return false;
            }

            for (int i = 0; i < 3; ++i)
            {
                int vIndex = SubTileVertexMap[sIndex0to15 * 3 + i];
                int vVal = (int)(naviMask >> (vIndex * 4)) & (int)MapSamplingData.HEIGHT_MASK;
                if (vVal == (int)MapSamplingData.VERTEX_MISSING_FLAG)
                {
                    return false;
                }
            }

            return true;
        }

        [BurstCompile]
        public static bool IsCircleOverlappingSquare(int3 pos, float2 circleCenter, float radius)
        {
            const float TILE_SIZE = 1f;
            float2 squareMin = new float2(pos.x, pos.z);
            float2 squareMax = squareMin + TILE_SIZE * new float2(1f, 1f);

            float2 closestPoint = math.clamp(circleCenter, squareMin, squareMax);
            float distanceSq = math.distancesq(closestPoint, circleCenter);

            return distanceSq <= radius * radius;
        }

        [BurstCompile]
        public static bool IsCircleOverlappingSubTile(int sIndex, float2 circleCenter, float radiusSq)
        {
            int vIdx0 = SubTileVertexMap[sIndex * 3 + 0];
            int vIdx1 = SubTileVertexMap[sIndex * 3 + 1];
            int vIdx2 = SubTileVertexMap[sIndex * 3 + 2];

            float2 p0 = VertexPositions[vIdx0];
            float2 p1 = VertexPositions[vIdx1];
            float2 p2 = VertexPositions[vIdx2];

            if (true == IsPointInTriangle(circleCenter, p0, p1, p2))
            {
                return true;
            }

            if (DistanceSqToSegment(p0, p1, circleCenter) <= radiusSq) return true;
            if (DistanceSqToSegment(p1, p2, circleCenter) <= radiusSq) return true;
            if (DistanceSqToSegment(p2, p0, circleCenter) <= radiusSq) return true;

            return false;

            static bool IsPointInTriangle(float2 p, float2 a, float2 b, float2 c)
            {
                float cp1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                float cp2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
                float cp3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);

                return (cp1 >= 0 && cp2 >= 0 && cp3 >= 0) || (cp1 <= 0 && cp2 <= 0 && cp3 <= 0);
            }
            static float DistanceSqToSegment(float2 a, float2 b, float2 p)
            {
                float2 ab = b - a;
                float2 ap = p - a;
                float t = math.dot(ap, ab) / math.dot(ab, ab);
                t = math.saturate(t);
                float2 closest = a + t * ab;
                return math.distancesq(p, closest);
            }
        }

        [BurstCompile]
        public static bool TryGetYInt(long linkMask, int dirIndex, out int yInt)
        {
            const int LINK_MASK = 0b11;
            int yMask = (int)(linkMask >> (dirIndex * 2)) & LINK_MASK;
            switch (yMask)
            {
                case 0b01: yInt = 0; break;
                case 0b10: yInt = 1; break;
                case 0b11: yInt = -1; break;
                default:
                    yInt = default;
                    return false;
            }

            return true;
        }

        [BurstCompile]
        public static int GetVertexIndexFromLocalPos(int localX, int localZ)
        {
            if (localZ == 0)
            {
                if (localX == 0) return 0;
                if (localX == 4) return 1;
                if (localX == 8) return 2;
            }
            else if (localZ == 2)
            {
                if (localX == 2) return 3;
                if (localX == 6) return 4;
            }
            else if (localZ == 4)
            {
                if (localX == 0) return 5;
                if (localX == 4) return 6;
                if (localX == 8) return 7;
            }
            else if (localZ == 6)
            {
                if (localX == 2) return 8;
                if (localX == 6) return 9;
            }
            else if (localZ == 8)
            {
                if (localX == 0) return 10;
                if (localX == 4) return 11;
                if (localX == 8) return 12;
            }

            return -1;
        }

        [BurstCompile]
        public static int GetHeightFromNaviMask(long naviMask, int vIndex)
        {
            if (vIndex < 0 || vIndex > 12)
            {
                return (int)MapSamplingData.VERTEX_MISSING_FLAG;
            }

            return (int)((naviMask >> (vIndex * 4)) & (int)MapSamplingData.HEIGHT_MASK);
        }
    }
}