#if UNITY_EDITOR

using Script.Data;
using System;
using Unity.Mathematics;
using UnityEngine;

/// <summary> 맵타일 관련하여 '에디터'에서만 사용하는 함수.
/// 비슷한 기능으로 함수가 많아지니까 헷갈려서 분리;
/// 개발하면서 함수 기능은 다시 정리하던가 그럽시다.
/// </summary>
public static partial class EditMapUtil
{
    public const int SPRITE_WIDTH   = 256;
    public const int SPRITE_HEIGHT  = 256;

    public const int TOTAL_BITS     = 13;
    public const int BITS_PER_CELL  = 4;
    public const int MATRIX_SIZE    = 5;

    public const int GRID_SIZE = 64;
    public const int SIZE_TILE = 8;
    public const int TILE_BITS = 6;
    public const int TILE_MASK = (1 << TILE_BITS) - 1;

    // (주의) map_tile_pivot != matrix.Origin(원점)
    public static readonly Vector2Int[] INDEX_MAP = new Vector2Int[]
    {
        new Vector2Int(0, 4), new Vector2Int(2, 4), new Vector2Int(4, 4),
        new Vector2Int(1, 3), new Vector2Int(3, 3),
        new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(4, 2),
        new Vector2Int(1, 1), new Vector2Int(3, 1),
        new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(4, 0)
    };

    public static long ComputeID(Vector3 worldPos)
    {
        int absTx = Mathf.FloorToInt(worldPos.x);
        int absTy = Mathf.FloorToInt(worldPos.y);
        int absTz = Mathf.FloorToInt(worldPos.z);

        int gX = Mathf.FloorToInt((float)absTx / GRID_SIZE);
        int gY = Mathf.FloorToInt((float)absTy / GRID_SIZE);
        int gZ = Mathf.FloorToInt((float)absTz / GRID_SIZE);

        int tX = absTx - gX * GRID_SIZE;
        int tY = absTy - gY * GRID_SIZE;
        int tZ = absTz - gZ * GRID_SIZE;

        if (tX < 0) { tX += GRID_SIZE; gX -= 1; }
        if (tY < 0) { tY += GRID_SIZE; gY -= 1; }
        if (tZ < 0) { tZ += GRID_SIZE; gZ -= 1; }

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
        return (((long)gKey) << 32) | (uint)tKey;
    }

    public static void ComputeKey(float3 worldPos, out int outGKey, out int outTKey)
    {
        int absTx = Mathf.FloorToInt(worldPos.x);
        int absTy = Mathf.FloorToInt(worldPos.y);
        int absTz = Mathf.FloorToInt(worldPos.z);

        int gX = Mathf.FloorToInt((float)absTx / GRID_SIZE);
        int gY = Mathf.FloorToInt((float)absTy / GRID_SIZE);
        int gZ = Mathf.FloorToInt((float)absTz / GRID_SIZE);

        int tX = absTx - gX * GRID_SIZE;
        int tY = absTy - gY * GRID_SIZE;
        int tZ = absTz - gZ * GRID_SIZE;

        if (tX < 0) { tX += GRID_SIZE; gX -= 1; }
        if (tY < 0) { tY += GRID_SIZE; gY -= 1; }
        if (tZ < 0) { tZ += GRID_SIZE; gZ -= 1; }

        outTKey = ((tX & TILE_MASK) << (TILE_BITS * 2))
                | ((tY & TILE_MASK) << (TILE_BITS * 1))
                | ((tZ & TILE_MASK) << (TILE_BITS * 0));

        byte bX = (byte)(sbyte)gX;
        byte bY = (byte)(sbyte)gY;
        byte bZ = (byte)(sbyte)gZ;

        outGKey = ((bX << 16) | (bY << 8) | bZ);
    }
    public static void ComputeKey(long id, out int outGKey, out int outTKey)
    {
        float3 position = ComputeWorldPosition(id);
        ComputeKey(position, out outGKey, out outTKey);
    }
    public static int ComputeGridKey(float3 worldPos)
    {
        int absTx = Mathf.FloorToInt(worldPos.x);
        int absTy = Mathf.FloorToInt(worldPos.y);
        int absTz = Mathf.FloorToInt(worldPos.z);

        int gX = Mathf.FloorToInt((float)absTx / GRID_SIZE);
        int gY = Mathf.FloorToInt((float)absTy / GRID_SIZE);
        int gZ = Mathf.FloorToInt((float)absTz / GRID_SIZE);

        byte bX = (byte)(sbyte)gX;
        byte bY = (byte)(sbyte)gY;
        byte bZ = (byte)(sbyte)gZ;

        // use only 3 bytes;
        return (bX << 16) | (bY << 8) | (bZ << 0);
    }

    public static float3 ComputeWorldPosition(long id)
    {
        int3 absPos = ComputeAbsoluteWorldPosition(id);
        return new Vector3(absPos.x, absPos.y, absPos.z);
    }
    private static int3 ComputeAbsoluteWorldPosition(long id)
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

    public static bool TryGetTileData(ConcurrentDictionary<int, EditMapGridData> map, long targetID, out EditMapTileData tile_data)
    {
        EditMapUtil.ComputeKey(targetID, out int grid_key, out int tile_key);
        if (false == map.ContainsKey(grid_key)
            || false == map[grid_key].TryGetTileData(tile_key, out tile_data))
        {
            tile_data = default;
            return false;
        }

        return true;
    }
    public static bool TryGetTileData(ConcurrentDictionary<int, EditMapGridData> map, float3 position, out EditMapTileData tile_data)
    {
        EditMapUtil.ComputeKey(position, out int grid_key, out int tile_key);
        if (false == map.ContainsKey(grid_key)
            || false == map[grid_key].TryGetTileData(tile_key, out tile_data))
        {
            tile_data = default;
            return false;
        }

        return true;
    }

    public static bool TryGetVerticeHeight(EditMapTileData tile, int vertice, out int heightx1000)
    {
        long mask = tile.NaviMask >> (Script.Index.MapTileIndex.HEIGHT_BITS * vertice);
        int maskInt = (int)mask & Script.Index.MapTileIndex.HEIGHT_MASK;
        if (0b1111 == maskInt)
        {
            heightx1000 = default;
            return false;
        }

        float pivotY = EditMapUtil.ComputeWorldPosition(tile.ID).y;
        heightx1000 = Mathf.RoundToInt((pivotY + maskInt * 0.125f) * 1000);
        return true;
    }
}
#endif