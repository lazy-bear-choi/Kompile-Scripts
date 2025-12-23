namespace Script.Manager
{
    using Script.Data;
    using Script.Index;
    using Script.IngameMessage;
    using Script.Util;
    using System.Linq;
    using System.Threading.Tasks;
    using Unity.Mathematics;
    using UnityEngine;

    public class FieldManager
    {
        private static ConcurrentDictionary<int, IngameMapGridObject> currentMapGrid;
        private static IngameFieldPlayer[] player_character = new IngameFieldPlayer[3];
        private static IngameMapTileData[] check_collide_tiles; // 아.. 충돌 확인 대상이구나.

        private static int current_layer_index = 0;
        public static int CurrentLayerIndex => current_layer_index;


        private static bool isSmall;
        private static float TileScale
        {
            get
            {
                return (true == isSmall) ? 0.5f : 1f;
            }
        }

        public async Task<bool> Initialize(PlayData playData)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[FieldManager] Initialize(PlayerData)");
#endif

            // instantiage map
            currentMapGrid = new ConcurrentDictionary<int, IngameMapGridObject>();


#if UNITY_EDITOR
            MapGridData grid_data;
            int[] test_grid = new int[] { 0, };
            //int[] test_grid = new int[] { 0, 1, 2, 3 };

            current_layer_index = 0;
            for (int i = 0; i < test_grid.Length; ++i)
            {
                grid_data = await AssetManager.ReadBinaryFileAsync<MapGridData>($"MapNavi_{test_grid[i]}");

                GameObject grid_obj = await AssetManager.GetOrNewInstanceAsync(AssetCode.MapGridPrefab, IngameManager.MapRootTransform);
                IngameMapGridObject root = grid_obj.GetComponent<IngameMapGridObject>();
                root.Initialize(grid_data);

                currentMapGrid.TryAdd(grid_data.Key, root);
            }
#endif

            // instantiage player unit
            GameObject obj           = await AssetManager.GetOrNewInstanceAsync(AssetCode.UnitBase, IngameManager.UnitRootTransform);
            player_character[0]      = obj.AddComponent<IngameFieldPlayer>();  // TODO: 테스트 목적이라서 나중에 다시 만들어야 함.
            IngameFieldPlayer player = player_character[0];

            check_collide_tiles = new IngameMapTileData[4];

            if (true == await player.Init(0))
            {
                IngameManager.InitFollowingCamera(player);
            }
            else
            {
                Debug.Assert(false, "[TEST] Fail to initialize player_character");
                return false;
            }

            MessageManager.Publish(new OnEndEvent(IngameEventType.FIELD_INIT));
            return true;
        }

        public static bool TryPlayerMove(Vector3 current_position, Vector3 move_delta, out float y)
        {
            y = 0f;
            if (false == TryGetMapTileData(current_position, out MapTileData current_tile))
            {
                return false;
            }

            Vector3 target_position = current_position + move_delta;

            // 목표 지점이 현재 지점과 같은 타일에 속하지 않는다면? 연결 지점의 타일을 찾아서 y값을 조정해야 한다
            if (false == MapUtil.IsKeyMaskEquals(current_position, target_position))
            {
                Vector3 target_pivot = MapUtil.GetTilePivotPosition(target_position, false);
                Vector3 current_pivot = MapUtil.GetTilePivotPosition(current_position, false);
                Vector3 diff = target_pivot - current_pivot;

                // 만약에 목표 지점으로 linked = false; 라면 이동 실패.
                if (false == MapUtil.TryGetLinkTileIndex(diff, out int index))
                {
                    return false;
                }

                if (false == MapUtil.TryGetLinkValue(current_tile.LinkMask, index, out y))
                {
                    return false;
                }

                // 여기서 개념이 좀 섞였구나. 분리 요망;
                //target_position += new Vector3(0f, linked_y, 0f);
            }

            if (false == TryGetLinkedTiles(target_position + new Vector3(0f, y, 0f)))
            {
                return false;
            }

            float radius = 0.3f; //얘도 거의 뭐 임시값이었네?
            bool isMovable = MapTileOverlapJobManager.Instance.CheckMapTileMovable(target_position, isSmall, radius, check_collide_tiles);
            if (false == isMovable)
            {
                return false;
            }

            // 다음 위치의 타일 정보 : IngameMapTileData target_tiles[0]; => GetTargetTiles(Vector3)에서 그렇게 정했음~!!
            IngameMapTileData targetTile = check_collide_tiles[0];

            int i = MapUtil.GetTriangleIndex(target_position, false);
            MapUtil.TryGetTrianglePoint(targetTile, i, 0, true, out float3 a);
            MapUtil.TryGetTrianglePoint(targetTile, i, 1, true, out float3 b);
            MapUtil.TryGetTrianglePoint(targetTile, i, 2, true, out float3 c);

            float pivot_y = targetTile.Pivot.y;
            y = MapUtil.CalculateYOnPlane(a, b, c, target_position.x, target_position.z);
            y = Mathf.Clamp(y, pivot_y, pivot_y + 1);
            return isMovable;
        }
        private static bool TryGetLinkedTiles(Vector3 target_position)
        {
            // 다음 이동할 목표 좌표에 대하여 타일값이 유효하게 존재하는가?
            if (false == TryGetMapTileData(target_position, out MapTileData mapTileData))
            {
                return false;
            }

            int grid_key = MapUtil.GetGridKeyMask(target_position);
            int tile_key = MapUtil.GetTileKeyMask(target_position);
            int index = 0;
            int target_link_mask = mapTileData.LinkMask;

            // 현재 위치한 타일++
            check_collide_tiles[index++] = new IngameMapTileData(grid_key, tile_key, mapTileData);

            // next_target_position을 기준으로 이웃한 타일이 어디인지 확인
            int quarant = MapUtil.GetQuarantInTile(target_position, isSmall);
            var link = quarant switch
            {
                0 => new int3(3, 4, 5),
                1 => new int3(5, 6, 7),
                2 => new int3(7, 0, 1),
                _ => new int3(1, 2, 3),
            };
            Vector3 tPivot = MapUtil.GetTilePivotPosition(target_position, isSmall);
            Vector3 neighbor_tile_pivot;

            for (int i = 0; i < 3; ++i)
            {
                int q = i switch
                {
                    0 => link.x,
                    1 => link.y,
                    _ => link.z
                };

                neighbor_tile_pivot = tPivot + TileScale * MapTileIndex.RELATIVE_COORD_BY_QUARANT[q];
                grid_key = MapUtil.GetGridKeyMask(neighbor_tile_pivot);
                tile_key = MapUtil.GetTileKeyMask(neighbor_tile_pivot);

                // 연결 여부 확인
                if (true == MapUtil.TryGetLinkValue(target_link_mask, q, out float y))
                {
                    neighbor_tile_pivot += y * Vector3.up;
                }
                else
                {
                    //연결 여부가 없다면? none_data 입력
                    check_collide_tiles[index++] = new IngameMapTileData(grid_key, tile_key);
                    continue;
                }

                // 타일 존재 확인
                if (true == TryGetMapTileData(neighbor_tile_pivot, out mapTileData))
                {
                    check_collide_tiles[index++] = new IngameMapTileData(grid_key, tile_key, mapTileData);
                }
                else
                {
                    // none_data
                    check_collide_tiles[index++] = new IngameMapTileData(grid_key, tile_key);
                }
            }

            return true;
        }

        public static bool TryGetMapTileData(float3 position, out MapTileData tile)
        {
            int gKey = MapUtil.GetGridKeyMask(position);
            if (false == currentMapGrid.ContainsKey(gKey))
            {
                tile = default;
                return false;
            }

            int tKey = MapUtil.GetTileKeyMask(position);
            return currentMapGrid[gKey].Data.TryGetTileData(tKey, out tile);
        }

        //public void Release()
        //{
        //    foreach (var grid in currentMapGrid.Values)
        //    {
        //        grid.Release();
        //    }
        //}
    }
}