#if UNITY_EDITOR
using Script.Data;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Hardware;
using UnityEngine;

public static partial class EditMapUtil //_LINK
{
    private static readonly int LINK_ZERO = 0b_01;
    private static readonly int LINK_UP   = 0b_10;
    private static readonly int LINK_DOWN = 0b_11;
    public  static readonly int LINK_NULL = 0b_00;

    private static readonly Dictionary<DirFlag, EditVertexIndexInfo> vertexIndex = new Dictionary<DirFlag, EditVertexIndexInfo>()
    {
        { DirFlag.LEFT,  new EditVertexIndexInfo(5, 0, 10) },
        { DirFlag.RIGHT, new EditVertexIndexInfo(7, 2, 12)},
        { DirFlag.UP,    new EditVertexIndexInfo(11, 10, 12) },
        { DirFlag.DOWN,  new EditVertexIndexInfo(1, 0, 2) },
    };
    private static readonly Dictionary<DirFlag, EditVertexIndexInfo> neighbor_vertex = new Dictionary<DirFlag, EditVertexIndexInfo>()
    {
        { DirFlag.LEFT,  new EditVertexIndexInfo(7, 2, 12) },
        { DirFlag.DOWN,  new EditVertexIndexInfo(11, 10, 12) },
        { DirFlag.UP,    new EditVertexIndexInfo(1, 0, 2)},
        { DirFlag.RIGHT, new EditVertexIndexInfo(5, 0, 10)}
    };
    private static readonly Dictionary<DirFlag, float3> DIRECTION = new Dictionary<DirFlag, float3>()
    {
        { DirFlag.LEFT,  new float3(-1f, 0f, 0f) },
        { DirFlag.RIGHT, new float3( 1f, 0f, 0f) },
        { DirFlag.UP,    new float3( 0f, 0f, 1f) },
        { DirFlag.DOWN,  new float3( 0f, 0f,-1f) },
    };

    public static (DirFlag, DirFlag) GetDirectionFlag(float x, float z)
    {
        DirFlag flag_x = DirFlag.NONE;
        DirFlag flag_z = DirFlag.NONE;

        if      (x > 0) { flag_x = DirFlag.RIGHT; }
        else if (x < 0) { flag_x = DirFlag.LEFT; }

        if      (z > 0) { flag_z = DirFlag.UP; }
        else if (z < 0) { flag_z = DirFlag.DOWN; }

        return (flag_x, flag_z);
    }

    public static bool TryGetLinkTileIndex(float2 dir, out int index)
    {
        DirFlag flag_x = DirFlag.NONE;
        DirFlag flag_z = DirFlag.NONE;

        if (dir.x > 0) { flag_x = DirFlag.RIGHT; }
        else if (dir.x < 0) { flag_x = DirFlag.LEFT; }

        // 사실은 z값
        if (dir.y > 0) { flag_z = DirFlag.UP; }
        else if (dir.y < 0) { flag_z = DirFlag.DOWN; }

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

    private static bool IsLinked(DirFlag direction_flag, EditMapTileData my_tile, EditMapTileData neighbor_tile)
    {
        EditVertexIndexInfo my_vertex_info = vertexIndex[direction_flag];
        EditVertexIndexInfo neighbor_vertex_info = neighbor_vertex[direction_flag];

        // 중앙점 비교
        if (false == my_tile.TryGetVerticeHeight(my_vertex_info.center, out int my_height_1000x)
            || false == neighbor_tile.TryGetVerticeHeight(neighbor_vertex_info.center, out int neighbor_height_1000x))
        {
            return false;
        }
        if (my_height_1000x != neighbor_height_1000x)
        {
            return false;
        }

        // 양옆의 점 높이 비교
        bool compare = false;
        if (true == my_tile.TryGetVerticeHeight(my_vertex_info.side0, out my_height_1000x)
            && true == neighbor_tile.TryGetVerticeHeight(neighbor_vertex_info.side0, out neighbor_height_1000x))
        {
            compare |= my_height_1000x == neighbor_height_1000x;
        }
        if (true == my_tile.TryGetVerticeHeight(my_vertex_info.side1, out my_height_1000x)
            && true == neighbor_tile.TryGetVerticeHeight(neighbor_vertex_info.side1, out neighbor_height_1000x))
        {
            compare |= my_height_1000x == neighbor_height_1000x;
        }

        return compare;
    }
    private static bool IsChainLinked(ConcurrentDictionary<int, EditMapGridData> map,
                                      EditMapTileData start_tile, EditMapTileData target_tile, 
                                      DirFlag dir_first, DirFlag dir_second)
    {
        float3 start_pivot = start_tile.GetTilePivot();
        float3 mid_pivot = start_pivot + DIRECTION[dir_first];
        float3 target_pivot = target_tile.GetTilePivot();

        if (false == EditMapUtil.TryGetTileData(map, mid_pivot, out EditMapTileData mid_tile))
        {
            return false;
        }

        bool start_to_mid = IsLinked(dir_first, start_tile, mid_tile);
        bool mid_to_target = IsLinked(dir_second, mid_tile, target_tile);

        return start_to_mid && mid_to_target; 
    }
    public static bool TryGetLinkMask(this EditMapTileData my_tile,
                                      ConcurrentDictionary<int, EditMapGridData> map,
                                      EditMapTileData neighbor_tile,
                                      float3 dir,
                                      out int my_link_mask,
                                      out int neighbor_link_mask)
    {
        bool isLinked;
        // 이걸 따로 받는게 나을 듯
        (DirFlag dir_x, DirFlag dir_z) = GetDirectionFlag(dir.x, dir.z);

        my_link_mask        = LINK_NULL;
        neighbor_link_mask  = LINK_NULL;

        DirFlag dir_mask = dir_x | dir_z;
        switch (dir_mask)
        {
            case DirFlag.LEFT:  // (-1, 0)
            case DirFlag.DOWN:  // ( 0,-1)
            case DirFlag.RIGHT: // ( 0, 1)
            case DirFlag.UP:    // ( 1, 0)
                isLinked = IsLinked(dir_x | dir_z, my_tile, neighbor_tile);
                break;

            case DirFlag.LEFT | DirFlag.DOWN:  // (-1,-1)
            case DirFlag.LEFT | DirFlag.RIGHT: // (-1, 1)
            case DirFlag.RIGHT | DirFlag.UP:   // ( 1, 1)
            case DirFlag.RIGHT | DirFlag.DOWN: // ( 1,-1)
                isLinked = IsChainLinked(map, my_tile, neighbor_tile, dir_x, dir_z)
                          && IsChainLinked(map, my_tile, neighbor_tile, dir_z, dir_x);
                break;

            default:
                return false;
        }

        if (true == isLinked)
        {
            switch (Mathf.RoundToInt(dir.y))
            {
                case 0:
                    my_link_mask = LINK_ZERO;
                    neighbor_link_mask = LINK_ZERO;
                    break;
                case 1:
                    my_link_mask = LINK_UP;
                    neighbor_link_mask = LINK_DOWN;
                    break;
                case -1:
                    my_link_mask = LINK_DOWN;
                    neighbor_link_mask = LINK_UP;
                    break;
                default:
                    return false;
            }

            int my_shift = GetLinkMaskShift(dir_mask);
            int neighbor_shift;
            switch (dir_mask)
            {
                case DirFlag.DOWN:
                    neighbor_shift = GetLinkMaskShift(DirFlag.UP);
                    break;
                case DirFlag.RIGHT:
                    neighbor_shift = GetLinkMaskShift(DirFlag.LEFT);
                    break;
                case DirFlag.UP:
                    neighbor_shift = GetLinkMaskShift(DirFlag.DOWN);
                    break;
                case DirFlag.LEFT:
                    neighbor_shift = GetLinkMaskShift(DirFlag.RIGHT);
                    break;

                case DirFlag.LEFT | DirFlag.DOWN:
                    neighbor_shift = GetLinkMaskShift(DirFlag.RIGHT | DirFlag.UP);
                    break;
                case DirFlag.RIGHT | DirFlag.DOWN:
                    neighbor_shift = GetLinkMaskShift(DirFlag.LEFT | DirFlag.UP);
                    break;
                case DirFlag.RIGHT | DirFlag.UP:
                    neighbor_shift = GetLinkMaskShift(DirFlag.LEFT | DirFlag.DOWN);
                    break;
                case DirFlag.LEFT | DirFlag.UP:
                    neighbor_shift = GetLinkMaskShift(DirFlag.RIGHT | DirFlag.DOWN);
                    break;
                default:
                    return false;
            }

            my_link_mask        <<= my_shift;
            neighbor_link_mask  <<= neighbor_shift;
        }

        return isLinked;
    }

    public static int GetLinkMaskShift(DirFlag flag)
    {
        // 반시계 방향으로 돌린다~!!
        return 2 * flag switch
        {
            DirFlag.LEFT | DirFlag.DOWN     => 0,
            DirFlag.DOWN                    => 1,
            DirFlag.RIGHT | DirFlag.DOWN    => 2,
            DirFlag.RIGHT                   => 3,
            DirFlag.RIGHT | DirFlag.UP      => 4,
            DirFlag.UP                      => 5,
            DirFlag.LEFT | DirFlag.UP       => 6,
            DirFlag.LEFT                    => 7,
            _ => -1
        };
    }


    // MapSampling.V2에서 사용
    public static DirFlag GetDirFlag(float x, float z)
    {
        DirFlag flag = DirFlag.NONE;

        if      (x > 0) { flag |= DirFlag.RIGHT; }
        else if (x < 0) { flag |= DirFlag.LEFT;  }

        if      (z > 0) { flag |= DirFlag.UP;   }
        else if (z < 0) { flag |= DirFlag.DOWN; }

        return flag;
    }
    public static EditVertexIndexInfo GetVertexIndexInfo(DirFlag flag)
    {
        return flag switch
        {
            DirFlag.LEFT  => new EditVertexIndexInfo( 5,  0, 10),
            DirFlag.RIGHT => new EditVertexIndexInfo( 7,  2, 12),
            DirFlag.UP    => new EditVertexIndexInfo(11, 10, 12),
            DirFlag.DOWN  => new EditVertexIndexInfo( 1,  0,  2),
            _ => default
        };
    }
    public static float3 GetDirectionVector(DirFlag flag)
    {
        return flag switch
        {
            DirFlag.LEFT    => new float3(-1f, 0f,  0f),
            DirFlag.RIGHT   => new float3( 1f, 0f,  0f),
            DirFlag.UP      => new float3( 0f, 0f,  1f),
            DirFlag.DOWN    => new float3( 0f, 0f, -1f),
            _ => default
        };
    }
}
#endif