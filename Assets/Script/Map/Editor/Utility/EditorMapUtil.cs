namespace Script.Map.Editor.Utility
{
    using UnityEngine;
    using Unity.Mathematics;
    using Script.Map.Runtime;
    using Script.Map.Editor.Data; // EditorMapTileDirFlag, EditorEdgeVertices가 있는 네임스페이스

    /// <summary>
    /// [Framework: Editor Utility]
    /// 에디터 베이킹 과정에서 필요한 순수 연산 및 상수 통합본입니다.
    /// (0,0,0) 원점 기반 좌표계를 준수하며, Job에서 호출 가능한 정적 메서드들을 제공합니다.
    /// </summary>
    public static class EditorMapUtil
    {
        // --- [Height Mask Rotation Constants] ---
        public const int TOTAL_BITS = 13;
        public const int BITS_PER_CELL = 4;
        public const int MATRIX_SIZE = 5;

        // --- [Map Spatial Partitioning & Bitwise Constants] ---
        public const int GRID_SIZE = 64;
        public const int SIZE_TILE = 8;
        public const int TILE_BITS = 6;
        public const int TILE_MASK = (1 << TILE_BITS) - 1;

        public static readonly Vector2Int[] INDEX_MAP = new Vector2Int[]
        {
            new Vector2Int(0, 4), new Vector2Int(2, 4), new Vector2Int(4, 4), // v00, v01, v02
            new Vector2Int(1, 3), new Vector2Int(3, 3),                       // v03, v04
            new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(4, 2), // v05, v06, v07
            new Vector2Int(1, 1), new Vector2Int(3, 1),                       // v08, v09
            new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(4, 0)  // v10, v11, v12
        };

        // =========================================================================
        // 1. 타일 ID 및 절대 좌표 변환 영역 (0,0,0 원점 기준)
        // =========================================================================

        public static float3 ComputeWorldPosition(long id)
        {
            int3 absPos = ComputeAbsoluteWorldPosition(id);
            return new float3(absPos.x, absPos.y, absPos.z);
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

        public static long ComputeTileID(float3 pivot)
        {
            int absTx = (int)math.floor(pivot.x);
            int absTy = (int)math.floor(pivot.y);
            int absTz = (int)math.floor(pivot.z);

            int gX = absTx >> 6;
            int gY = absTy >> 6;
            int gZ = absTz >> 6;

            int tX = absTx & TILE_MASK;
            int tY = absTy & TILE_MASK;
            int tZ = absTz & TILE_MASK;

            uint tKey = (uint)((tX & TILE_MASK) << (TILE_BITS * 2)) |
                        (uint)((tY & TILE_MASK) << (TILE_BITS * 1)) |
                        (uint)((tZ & TILE_MASK) << (TILE_BITS * 0));

            byte bX = (byte)(sbyte)gX;
            byte bY = (byte)(sbyte)gY;
            byte bZ = (byte)(sbyte)gZ;

            uint gKey = (uint)((bX << 16) | (bY << 8) | bZ);

            return (((long)gKey) << 32) | tKey;
        }

        // =========================================================================
        // 2. 타일 간 기하학적 연결(Link) 및 방향 판정 영역
        // =========================================================================

        /// <summary>
        /// 2D 방향 벡터(x, y)를 입력받아 EditorMapTileDirFlag(비트 플래그)로 변환합니다.
        /// </summary>
        public static EditorMapTileDirFlag GetDirFlag(float x, float y)
        {
            EditorMapTileDirFlag flag = EditorMapTileDirFlag.NONE;

            if (x < -0.1f) flag |= EditorMapTileDirFlag.LEFT;
            if (x > 0.1f) flag |= EditorMapTileDirFlag.RIGHT;
            if (y < -0.1f) flag |= EditorMapTileDirFlag.DOWN; // 3D에서는 -Z 방향
            if (y > 0.1f) flag |= EditorMapTileDirFlag.UP;   // 3D에서는 +Z 방향

            return flag;
        }

        /// <summary>
        /// 특정 방향(DirFlag)이 LinkMask 내에서 몇 번째 비트 위치(Shift)를 가지는지 반환합니다.
        /// 8방향 기준 2비트씩 할당하여 16비트(ushort) 마스크를 구성합니다.
        /// </summary>
        public static int GetLinkMaskShift(EditorMapTileDirFlag dirFlag)
        {
            return dirFlag switch
            {
                EditorMapTileDirFlag.UP => 0,
                EditorMapTileDirFlag.UP | EditorMapTileDirFlag.RIGHT => 2,
                EditorMapTileDirFlag.RIGHT => 4,
                EditorMapTileDirFlag.DOWN | EditorMapTileDirFlag.RIGHT => 6,
                EditorMapTileDirFlag.DOWN => 8,
                EditorMapTileDirFlag.DOWN | EditorMapTileDirFlag.LEFT => 10,
                EditorMapTileDirFlag.LEFT => 12,
                EditorMapTileDirFlag.UP | EditorMapTileDirFlag.LEFT => 14,
                _ => 0
            };
        }

        /// <summary>
        /// 특정 방향(상하좌우)에 위치한 타일 경계면의 3개 정점(Center, Side0, Side1) 인덱스를 반환합니다.
        /// 13정점(v00~v12) 구조에 맞춰 하드코딩된 인덱스를 제공합니다.
        /// </summary>
        public static EditorEdgeVertices GetEdgeVertices(EditorMapTileDirFlag direction)
        {
            return direction switch
            {
                // 왼쪽 경계면 (x=0 축: v00, v05, v10)
                EditorMapTileDirFlag.LEFT => new EditorEdgeVertices(5, 0, 10),

                // 오른쪽 경계면 (x=1 축: v02, v07, v12)
                EditorMapTileDirFlag.RIGHT => new EditorEdgeVertices(7,2,12),

                // 위쪽 경계면 (+Z 축: v10, v11, v12)
                EditorMapTileDirFlag.UP => new EditorEdgeVertices(11,10,12),

                // 아래쪽 경계면 (-Z 축: v00, v01, v02)
                EditorMapTileDirFlag.DOWN => new EditorEdgeVertices(1,0,2),

                _ => new EditorEdgeVertices(-1,-1,-1)
            };
        }

        /// <summary>
        /// 방향 비트 플래그를 실제 3D 월드 공간의 방향 벡터(float3)로 변환합니다.
        /// </summary>
        public static float3 GetDirectionVector(EditorMapTileDirFlag dirFlag)
        {
            float3 dir = float3.zero;

            if ((dirFlag & EditorMapTileDirFlag.LEFT) != 0) dir.x -= 1f;
            if ((dirFlag & EditorMapTileDirFlag.RIGHT) != 0) dir.x += 1f;
            if ((dirFlag & EditorMapTileDirFlag.DOWN) != 0) dir.z -= 1f; // 2D의 DOWN은 3D의 -Z
            if ((dirFlag & EditorMapTileDirFlag.UP) != 0) dir.z += 1f; // 2D의 UP은 3D의 +Z

            return dir;
        }

        public static void ComputeKey(float3 worldPos, out int outGKey, out int outTKey)
        {
            // C#에서 음수에 대한 나눗셈 버림(Floor)을 정확히 처리하기 위해 Mathf.FloorToInt 사용
            int absTx = Mathf.FloorToInt(worldPos.x);
            int absTy = Mathf.FloorToInt(worldPos.y);
            int absTz = Mathf.FloorToInt(worldPos.z);

            // 음수 좌표를 안전하게 Grid 공간으로 매핑
            int gX = Mathf.FloorToInt((float)absTx / GRID_SIZE);
            int gY = Mathf.FloorToInt((float)absTy / GRID_SIZE);
            int gZ = Mathf.FloorToInt((float)absTz / GRID_SIZE);

            // 타일 지역 좌표(0~63) 계산
            int tX = absTx - gX * GRID_SIZE;
            int tY = absTy - gY * GRID_SIZE;
            int tZ = absTz - gZ * GRID_SIZE;

            // 만약 음수 영역이라면, GridKey와 TileKey를 보정
            if (tX < 0) { tX += GRID_SIZE; gX -= 1; }
            if (tY < 0) { tY += GRID_SIZE; gY -= 1; }
            if (tZ < 0) { tZ += GRID_SIZE; gZ -= 1; }

            outTKey = ((tX & TILE_MASK) << (TILE_BITS * 2))
                        | ((tY & TILE_MASK) << (TILE_BITS * 1))
                        | ((tZ & TILE_MASK) << (TILE_BITS * 0));

            byte bX = (byte)(sbyte)gX;
            byte bY = (byte)(sbyte)gY;
            byte bZ = (byte)(sbyte)gZ;

            outGKey = (bX << 16) | (bY << 8) | bZ;
        }
    }
}