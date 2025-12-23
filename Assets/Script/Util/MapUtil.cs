namespace Script.Util
{
    using Script.Data;
    using System;
    using Unity.Mathematics;
    using UnityEngine;
    using static Index.MapTileIndex;

    public static partial class MapUtil
    {
        [Flags]
        public enum DirFlag
        {
            NONE = 0,
            UP = 1,
            DOWN = 1 << 1,
            LEFT = 1 << 2,
            RIGHT = 1 << 3
        }

        public static int GetGridKeyMask(float3 position)
        {
            int mask = 0;
            int gx = Mathf.FloorToInt(position.x / GRID_SIZE);
            int gy = Mathf.FloorToInt(position.y / GRID_SIZE);
            int gz = Mathf.FloorToInt(position.z / GRID_SIZE);

            if (gx < 0)
            {
                mask |= 1 << SHIFT_GRID_X_SIGN;
                gx *= -1;
            }
            mask |= gx << SHIFT_GRID_X;

            if (gy < 0)
            {
                mask |= 1 << SHIFT_GRID_Y_SIGN;
                gy *= -1;
            }
            mask |= gy << SHIFT_GRID_Y;

            if (gz < 0)
            {
                mask |= 1 << SHIFT_GRID_Z_SIGN;
                gz *= -1;
            }
            mask |= gz << SHIFT_GRID_Z;

            return mask;
        }
        public static int GetTileKeyMask(float3 position)
        {
            int x = Mathf.FloorToInt(position.x % GRID_SIZE);
            if (x < 0)
            {
                x += GRID_SIZE;
            }

            int y = Mathf.FloorToInt(position.y % GRID_SIZE);
            if (y < 0)
            {
                y += GRID_SIZE;
            }

            int z = Mathf.FloorToInt(position.z % GRID_SIZE);
            if (z < 0)
            {
                z += GRID_SIZE;
            }

            int tileKeyMask = 0;
            tileKeyMask |= x << SHIFT_TILE_X;
            tileKeyMask |= y << SHIFT_TILE_Y;
            tileKeyMask |= z << SHIFT_TILE_Z;

            return tileKeyMask;
        }
        public static bool TryGetLinkValue(int link_mask, int q, out float y)
        {
            y = default;
            int mask = (link_mask >> (q * 2)) & 0b_11;

            switch (mask)
            {
                case 0b_01: y =  0f; break;
                case 0b_10: y =  1f; break;
                case 0b_11: y = -1f; break;
                default:
                    return false;
            }

            return true;
        }

        public static bool IsKeyMaskEquals(this Vector3 position, Vector3 compare)
        {
            // compare grid key
            int my_grid_key      = GetGridKeyMask(position);
            int compare_grid_key = GetGridKeyMask(compare);
            if (my_grid_key != compare_grid_key)
            {
                return false;
            }

            // compare tile key
            int my_tile_key      = GetTileKeyMask(position);
            int compare_tile_key = GetTileKeyMask(compare);

            return my_tile_key == compare_tile_key;
        }
        public static bool TryGetLinkTileIndex(Vector3 diff, out int index)
        {
            DirFlag flag_x = DirFlag.NONE;
            DirFlag flag_z = DirFlag.NONE;

            if      (diff.x > 0) { flag_x = DirFlag.RIGHT; }
            else if (diff.x < 0) { flag_x = DirFlag.LEFT; }

            if      (diff.z > 0) { flag_z = DirFlag.UP; }
            else if (diff.z < 0) { flag_z = DirFlag.DOWN; }

            index = -1;
            switch (flag_x | flag_z)
            {
                case DirFlag.DOWN | DirFlag.LEFT:   index = 0; break;
                case DirFlag.DOWN:                  index = 1; break;
                case DirFlag.DOWN | DirFlag.RIGHT:  index = 2; break;
                case DirFlag.RIGHT:                 index = 3; break;
                case DirFlag.UP | DirFlag.RIGHT:    index = 4; break;
                case DirFlag.UP:                    index = 5; break;
                case DirFlag.UP | DirFlag.LEFT:     index = 6; break;
                case DirFlag.LEFT:                  index = 7; break;
                default:
                    return false;
            }

            return true;
        }

        public static float3 GetNeighborPivotOffset(int index)
        {
            switch (index)
            {
                case 0: return new float3(-1f,  0f, -1f);
                case 1: return new float3( 0f,  0f, -1f);
                case 2: return new float3( 1f,  0f, -1f);
                case 3: return new float3( 1f,  0f,  0f);
                case 4: return new float3( 1f,  0f,  1f);
                case 5: return new float3( 0f,  0f,  1f);
                case 6: return new float3(-1f,  0f,  1f);
                case 7: return new float3(-1f,  0f,  0f);
                default:
                    break;
            }

            return default;
        }
        public static long GetTileNodeKey(float3 position)
        {
            int adjGKey = GetGridKeyMask(position);
            int adjTKey = GetTileKeyMask(position);
            
            return ((long)adjGKey << 32) | (uint)adjTKey;
        }
    }


    // 잠시 킵...
    public static partial class MapUtil
    {
        public static Vector3 GetTilePivotPosition(Vector3 position, bool isSmall)
        {
            float x, y, z;
            x = Mathf.Floor(position.x);
            y = Mathf.Floor(position.y);
            z = Mathf.Floor(position.z);

            // small-scale 타일은 0.5f 간격으로 pivot이 있음 (크기가 0.5f * 0.5f)
            // position의 소수점이 0.5f 이상이면 pivot은 ~.5f이고 반대라면 ~.0f가 pivot이 된다.
            if (true == isSmall)
            {
                float size = 0.5f;

                x += (position.x % 1f >= size) ? size : 0f;
                y += (position.y % 1f >= size) ? size : 0f;
                z += (position.z % 1f >= size) ? size : 0f;
            }

            return new Vector3(x, y, z);
        }
        public static float3 GetTilePivotPosition(int gKey, int tKey)
        {
            return default;
        }

        public static int GetQuarantInTile(Vector3 position, bool isSmall)
        {
            Vector3 tilePivot = GetTilePivotPosition(position, isSmall);
            Vector3 diff = position - tilePivot;

            float scale = (true == isSmall) ? 0.5f : 1f;
            float halfTileSize = 0.5f * scale;

            if (diff.z >= halfTileSize)
            {
                if (diff.x >= halfTileSize) { return 0; }
                else { return 1; }
            }
            else
            {
                if (diff.x >= halfTileSize) { return 3; }
                else { return 2; }
            }
        }

        public static bool TryGetTrianglePoint(IngameMapTileData data, int tri_index, int vertice, bool getY, out float3 point)
        {
            int pt_virtual_index = TriangleVertex[tri_index * 3 + vertice];
            int pt_height_mask = (int)((data.NaviMask >> pt_virtual_index * 4) & 0b_1111);

            // 삼각형을 만들 수 있는지 여부는 따로 확인
            bool set_triangle = pt_height_mask <= 0b_1000;

            // 삼각형을 만들 수 없다면 추가로 더할 높이값을 0으로 처리
            float y = 0f;
            if (true == getY
                && true == set_triangle)
            {
                y = pt_height_mask * 0.125f;

            }

            float x, z;
            switch (pt_virtual_index)
            {
                case 0: x = 0.00f; z = 0.00f; break;
                case 1: x = 0.50f; z = 0.00f; break;
                case 2: x = 1.00f; z = 0.00f; break;
                case 3: x = 0.25f; z = 0.25f; break;
                case 4: x = 0.75f; z = 0.25f; break;
                case 5: x = 0.00f; z = 0.50f; break;
                case 6: x = 0.50f; z = 0.50f; break;
                case 7: x = 1.00f; z = 0.50f; break;
                case 8: x = 0.25f; z = 0.75f; break;
                case 9: x = 0.75f; z = 0.75f; break;
                case 10: x = 0.00f; z = 1.00f; break;
                case 11: x = 0.50f; z = 1.00f; break;
                case 12: x = 1.00f; z = 1.00f; break;
                default:
                    point = default;
                    return false;
            }

            // 해당 삼각형 구역이 겹치는지 여부를 확인하고자 point는 넘긴다.
            point = data.Pivot + new Vector3(x, y, z);

            return set_triangle;
        }

        public static int GetTriangleIndex(Vector3 position, bool isSmall)
        {
            Vector3 tilePivot = MapUtil.GetTilePivotPosition(position, isSmall);
            Vector3 diff = position - tilePivot;

            float x = Mathf.Round(diff.x * 100_000f) * 0.0_0001f;
            float z = Mathf.Round(diff.z * 100_000f) * 0.0_0001f;

            int index = 0;
            index += (x >= 0.5f) ? 4 : 0;
            index += (z >= 0.5f) ? 8 : 0;

            x = Mathf.Round((x % 0.5f) * 100_000f) * 0.0_0001f;
            z = Mathf.Round((z % 0.5f) * 100_000f) * 0.0_0001f;

            bool zEx = z >= x;
            bool zEnx = z >= -x + 0.5f;

            if      (!zEx & zEnx)   { index += 1; }
            else if (zEx & zEnx)    { index += 2; }
            else if (zEx & !zEnx)   { index += 3; }

            return index;
        }

        public static float CalculateYOnPlane(float3 a, float3 b, float3 c, float tx, float tz)
        {
            // 평면을 정의하는 두 벡터를 구합니다.
            float3 ab = b - a;
            float3 ac = c - a;

            // 두 벡터의 외적을 통해 평면의 법선 벡터를 구합니다.
            float3 normal = math.cross(ab, ac);

            // 평면의 방정식: A(x - x0) + B(y - y0) + C(z - z0) = 0
            // 법선 벡터의 성분이 A, B, C가 됩니다.
            float nx = normal.x;
            float ny = normal.y;
            float nz = normal.z;

            // B가 0인 경우, 평면이 y축에 수직이므로 이 방법으로는 y값을 계산할 수 없습니다.
            // 이 경우, 사용자의 입력 또는 전제에 문제가 있을 수 있습니다.
            if (Mathf.Abs(ny) < 1e-6)
            {
                Debug.LogError("The plane is parallel to the Y-axis. Cannot solve for y.");
                return float.NaN; // 유효하지 않은 값을 반환
            }

            // 평면의 방정식에 (tx, ty, tz)를 대입하고 ty에 대해 정리합니다.
            // A * (tx - a.x) + B * (ty - a.y) + C * (tz - a.z) = 0
            // B * (ty - a.y) = -A * (tx - a.x) - C * (tz - a.z)
            // ty - a.y = (-A * (tx - a.x) - C * (tz - a.z)) / B
            // ty = a.y + (-A * (tx - a.x) - C * (tz - a.z)) / B

            float ty = a.y + (-nx * (tx - a.x) - nz * (tz - a.z)) / ny;
            return Mathf.Round(ty * 100_000) * 0.00_001f;
        }
    }
}
