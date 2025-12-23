namespace Script.Map
{
    using Unity.Burst;
    using Unity.Mathematics;
    using UnityEngine;

    // 참고: STUDY_PositionKeyUtil
    public static class MapPathUtil
    {
        public const int GRID_SIZE = 64;
        public const int TILE_BITS = 6;
        public const int TILE_MASK = (1 << TILE_BITS) - 1;
        public const int NONE_SUBTILE = 0b1111;

        private static readonly int[] SubTileVertexMap = new int[]
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

        public static long ComputeID(int gKey, int tKey)
        {
            const int SHFIT = 32;
            return ((long)gKey << SHFIT) | (uint)tKey;
        }
        public static Vector3 ComputeWorldPosition(long id)
        {
            int3 absPos = ComputeAbsoluteWorldPosition(id);
            return new Vector3(absPos.x, absPos.y, absPos.z);
        }
        public static int3 ComputeAbsoluteWorldPosition(long id)
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

        [BurstCompile]
        public static bool IsSubTileValid(long naviMask, int sIndex0to15)
        {
            if (sIndex0to15 < 0 || sIndex0to15 > 15)
            {
                return false;
            }

            // 3개의 정점을 순회하며 유효성 체크
            int offset = sIndex0to15 * 3;
            for (int i = 0; i < 3; ++i)
            {
                int vIndex = SubTileVertexMap[offset + i];

                // 4비트씩 시프트하여 높이값 추출
                int vVal = (int)((naviMask >> (vIndex * 4)) & 0b1111);
                if (NONE_SUBTILE == vVal)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 월드 좌표를 서브 타일 인덱스(0..15)로 변환</br>
        /// tile.pivot 으로부터 상대 거리를 구해서 정점의 인덱스를 구하는 방식
        /// </summary>
        [BurstCompile]
        public static int GetSubTileIndex(float3 worldPos)
        {
            float localX = worldPos.x - math.floor(worldPos.x);
            float localZ = worldPos.z - math.floor(worldPos.z);

            // 사분면의 기준 인덱스:
            // 각 사분면의 서브 타일의 시작 인덱스는 (s0, s4, s8, s12)이다
            int col = (localX >= 0.5f) ? 1 : 0;
            int row = (localZ >= 0.5f) ? 1 : 0;
            int baseIndex = (row * 8) + (col * 4);

            // 사분면 내에서의 로컬 중심 좌표 계산:
            // 각 사분면은 0.5*0.5 크기이며 중심은 (0.25, 0.25)이다.
            float quadCenterX = (col * 0.5f) + 0.25f;
            float quadCenterZ = (row * 0.5f) + 0.25f;

            // 중심으로부터의 오차
            float dx = localX - quadCenterX;
            float dz = localZ - quadCenterZ;

            // (사분면의 중점으로부터 worldPos까지의 거리의 방향의) 절대값을 비교하여
            // 가로형(좌/우), 세로형(상/하)인지 판단
            int offset;
            if (math.abs(dx) > math.abs(dz))
            {
                // 가로형: dx가 양수면 오른쪽(right)
                offset = (dx > 0) ? 1 : 3;
            }
            else
            {
                // 세로형: dz가 양수면 위쪽(top)
                offset = (dz > 0) ? 2 : 0;
            }

            return baseIndex + offset;
        }
    }
}