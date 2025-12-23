#if UNITY_EDITOR
namespace Script.Editor.MapSampling
{
    using Script.Data;
    using Script.Manager;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;

    public partial class EditMapTileSampling : MonoBehaviour
    {
        private class CombineMeshData
        {
            public List<CombineInstance> combineInstances;
            public List<Vector2> combinedUVs;
            public int vertexCount;
            public int index;
        }

        private const int       VERTEX_LIMIT        = 65535;

        private readonly string assetGroupName      = "MapRender";
        private readonly string MAP_NAVI_DATA_PATH  = "Rcs\\Bin\\MapNavRawData";
        private readonly float2[] dir               = new float2[]
        {
            new float2( 1, -1),
            new float2( 1,  1),
            new float2(-1,  1),
            new float2(-1, -1),

            new float2( 0, -1),
            new float2( 1,  0),
            new float2( 0,  1),
            new float2(-1,  0),
        };
        private readonly float[] ny                 = new float[] { 0, 1, -1 };

        [SerializeField] private Transform instanceTransform;
        [SerializeField] byte sceneIndex = 0;

        public byte SceneIndex => sceneIndex;
        public Transform MapRoot => instanceTransform;

        private ConcurrentDictionary<int, EditMapGridData> map;
        public ConcurrentDictionary<int, EditMapGridData> Map => map;

        public void Bake()
        {
            Debug.Log($"--- Start Baking Map ---");

            // set data
            EditMapTileObject[] tiles = instanceTransform.GetComponentsInChildren<EditMapTileObject>();

            // JobSystem -> EditMapData 일괄 생성
            int length = tiles.Length;
            Allocator allocation_type = Allocator.TempJob;

            var native_array_scene_index    = new NativeArray<byte>(length, allocation_type);
            var native_array_render_layer   = new NativeArray<ushort>(length, allocation_type);
            var native_array_position       = new NativeArray<float3>(length, allocation_type);
            var native_array_rotateY        = new NativeArray<float>(length, allocation_type);
            var native_array_heights        = new NativeArray<ulong>(length, allocation_type);
            var native_array_result         = new NativeArray<EditMapTileData>(length, allocation_type);

            EditMapTileObject tileData;
            for (int i = 0; i < tiles.Length; i++)
            {
                tileData = tiles[i];

                native_array_scene_index[i]     = sceneIndex;
                native_array_render_layer[i]    = tileData.RenderLayer;
                native_array_position[i]        = new float3(tileData.transform.position.x, tileData.transform.position.y, tileData.transform.position.z);
                native_array_rotateY[i]         = tileData.transform.eulerAngles.y;
                native_array_heights[i]         = tileData.HeightMask;
            }

            EditMapTileJob job = new EditMapTileJob
            {
                SceneIndex  = native_array_scene_index,
                RenderLayer = native_array_render_layer,
                Position    = native_array_position,
                RotY        = native_array_rotateY,
                Height      = native_array_heights,
                Data        = native_array_result
            };
            JobHandle jobHandle = job.Schedule(tiles.Length, 64);
            jobHandle.Complete();

            // Map 등록
            int renderIndex;
            long naviMask;

            map = new ConcurrentDictionary<int, EditMapGridData>();
            for (int i = 0; i < native_array_result.Length; ++i)
            {
                EditMapUtil.ComputeKey(native_array_result[i].ID, out int gridKey, out int tileKey);
                naviMask    = native_array_result[i].NaviMask;
                renderIndex = native_array_result[i].RenderIndex;

                if (false == map.ContainsKey(gridKey))
                {
                    map.TryAdd(gridKey, new EditMapGridData(gridKey));
                }

                EditMapTileData tile_data = new EditMapTileData()
                {
                    ID          = EditMapUtil.ComputeID(gridKey, tileKey),
                    NaviMask    = naviMask,
                    LinkMask    = default, // 나중에 Link() 할 때에 데이터 입력
                    RenderIndex  = (ushort)renderIndex
                };

                map[gridKey].TryAdd(tileKey, tile_data);
            }

            long startID = native_array_result[0].ID;

            native_array_scene_index .Dispose();
            native_array_render_layer.Dispose();
            native_array_position    .Dispose();
            native_array_rotateY     .Dispose();
            native_array_heights     .Dispose();
            native_array_result      .Dispose();

            LinkTiles(map, startID);    // DFS 알고리즘 -> EditMaplinkMask 생성
            CombineMapMeshes(map, tiles); // 맵 매쉬 합치기

            foreach (KeyValuePair<int, EditMapGridData> grid in map)
            {
                MapGridData mapGridData = new MapGridData()
                {
                    Key              = grid.Key,
                    NaviTileDict = grid.Value.ParseData(),
                    layerMeshAssets          = grid.Value.LayerMeshAssets
                };

                AssetManager.WriteBinaryFile<MapGridData>(
                    data: mapGridData,
                    dataPath: MAP_NAVI_DATA_PATH,
                    fileName: $"MapNavi_{mapGridData.Key}",
                    addressableGroup: "MapNavi");
            }

            Debug.Log($"--- End (length: {tiles.Length}) ---");
            System.GC.Collect();
        }

        private void LinkTiles(ConcurrentDictionary<int, EditMapGridData> map, long startID)
        {
            Stack<long> stack     = new Stack<long>();
            HashSet<long> visited = new HashSet<long>();

            stack.Push(startID);

            while (stack.Count > 0)
            {
                float3 target_pivot = stack.Pop();
                if (false == EditMapUtil.TryGetTileData(map, target_pivot, out EditMapTileData visit_tile))
                {
                    continue;
                }

                for (int i = 0; i < dir.Length; ++i)
                {
                    // 이미 연결함
                    if (true == visit_tile.IsLinked(dir[i])) // TODO: 얘도 v2에 맞춰서 바꿔야겠네;
                    {
                        continue;
                    }

                    for (int y = 0; y < ny.Length; ++y)
                    {
                        float3 target_dir = new float3(dir[i].x, ny[y], dir[i].y);
                        long neighborID = EditMapUtil.ComputeID(target_pivot + target_dir);

                        // 이미 방문했다면 pass
                        if (true == visited.Contains(neighborID))
                        {
                            break;
                        }

                        // 타일이 없다면? 다른 y값으로 탐색 이어서
                        if (false == EditMapUtil.TryGetTileData(map, neighborID, out EditMapTileData neighbor_tile))
                        {
                            continue;
                        }

                        // 이번에 방문했습니다^^ 추가
                        visited.Add(neighborID);

                        // 인접한 타일 -> 다음 탐색에 추가
                        stack.Push(neighbor_tile.ID);

                        // 인접한 타일과 연결되었다면 추가
                        if (true == visit_tile.TryGetLinkMask(map, neighbor_tile, target_dir, out int my_link_mask, out int neighbor_link_mask))
                        {
                            EditMapUtil.ComputeKey(visit_tile.ID, out int gKey, out int tKey);
                            visit_tile = new EditMapTileData(visit_tile, my_link_mask);
                            map[gKey].Data[tKey] = visit_tile;

                            EditMapUtil.ComputeKey(neighbor_tile.ID, out gKey, out tKey);
                            neighbor_tile = new EditMapTileData(neighbor_tile, neighbor_link_mask);
                            map[gKey].Data[tKey] = neighbor_tile;

                            break;
                        }
                    }
                }
            }
        }
        private void CombineMapMeshes(ConcurrentDictionary<int, EditMapGridData> map, EditMapTileObject[] tiles)
        {
            Dictionary<long, CombineMeshData> tempDataDict = new Dictionary<long, CombineMeshData>();
            EditMapTileObject tile;
            CombineMeshData tempData;

            for (int i = 0; i < tiles.Length; ++i)
            {
                tile = tiles[i];
                //https://www.youtube.com/watch?v=RAwRNE1SJC8 uint 값을 넘어가는 시프팅은 연산이 다르다고 함;
                long temp_grid_key = ((long)tile.RenderLayer << 32) | ((long)sceneIndex << 24) | (long)tile.GridKey;
                if (false == tempDataDict.ContainsKey(temp_grid_key))
                {
                    tempDataDict[temp_grid_key] = new CombineMeshData
                    {
                        combineInstances = new List<CombineInstance>(),
                        combinedUVs = new List<Vector2>(),
                        vertexCount = 0,
                        index = 0
                    };
                }

                tempData = tempDataDict[temp_grid_key];

                if (tile.MeshFilter == null || tile.MeshFilter.sharedMesh == null)
                {
                    Debug.LogWarning($"Tile at index {i} has a null MeshFilter or sharedMesh. Skipping.");
                    continue; // 빈 메쉬는 처리하지 않고 건너뜁니다.
                }

                int currentVertexCount = tempData.vertexCount;
                int tileVertexCount = tile.MeshFilter.sharedMesh.vertexCount;

                // 정점 0개인 메쉬는 건너뛴다.
                if (tileVertexCount == 0)
                {
                    Debug.LogWarning($"Tile at index {i} has 0 vertices. Skipping.");
                    continue;
                }

                if (currentVertexCount + tileVertexCount > VERTEX_LIMIT)
                {
                    Mesh combinedMesh = new Mesh();
                    combinedMesh.CombineMeshes(tempData.combineInstances.ToArray(), true, true);
                    combinedMesh.uv = tempData.combinedUVs.ToArray();

                    if (combinedMesh.vertexCount != tempData.combinedUVs.Count)
                    {
                        Debug.LogError($"[227] UV/Vertex Count Mismatch! Vertices: {combinedMesh.vertexCount}, UVs: {tempData.combinedUVs.Count}");
                    }

                    SaveMeshAsset(map, combinedMesh, sceneIndex, tile.GridKey, tile.RenderLayer, tempData.index, true, false);

                    tempData.combineInstances.Clear();
                    tempData.combinedUVs.Clear();
                    tempData.vertexCount = 0;
                    tempData.index++;
                }

                CombineInstance combInstance = new CombineInstance()
                {
                    mesh = tile.MeshFilter.sharedMesh,
                    transform = tile.transform.localToWorldMatrix
                };

                Vector2[] uvs = GetUVs(combInstance, tile.TextureIndex);

                if (uvs.Length != tileVertexCount)
                {
                    Debug.LogError($"Tile {i} - Inconsistent UV/Vertex count. Mesh Vertices: {tileVertexCount}, GetUVs returned: {uvs.Length}");
                }

                tempData.combineInstances.Add(combInstance);
                tempData.combinedUVs.AddRange(uvs);
                tempData.vertexCount += tileVertexCount;

                tempDataDict[temp_grid_key] = tempData;
            }

            // 마지막까지 남은 데이터를 긁어모아 생성
            foreach (var kvp in tempDataDict)
            {
                tempData = kvp.Value;
                if (tempData.combineInstances.Count > 0)
                {
                    Mesh combinedMesh = new Mesh();
                    combinedMesh.CombineMeshes(tempData.combineInstances.ToArray(), true, true);
                    combinedMesh.uv = tempData.combinedUVs.ToArray();

                    if (combinedMesh.vertexCount != tempData.combinedUVs.Count)
                    {
                        Debug.LogError($"[266] UV/Vertex Count Mismatch! Vertices: {combinedMesh.vertexCount}, UVs: {tempData.combinedUVs.Count}");
                    }

                    // long key = tile.RenderLayer << 32 | sceneIndex << 24 | tile.GridKey;
                    int gridKey = (int)(kvp.Key & 0xFFFF);
                    int layerMask = (int)(kvp.Key >> 32);
                    SaveMeshAsset(map, combinedMesh, sceneIndex, gridKey, layerMask, tempData.index, true, false);
                }
            }

            EditorUtility.SetDirty(AddressableAssetSettingsDefaultObject.Settings);
            AssetDatabase.Refresh();


            // included methods
            Vector2[] GetUVs(CombineInstance target, int textureIndex)
            {
                // for test? 이거 맞겠지?
                float spriteSize = 256f;
                int altasWidth = 2048;
                int altasHeight = 2048;

                // atlas 내 몇 칸으로 배치되었는지 계산 (좌측 하단 기준)
                int atlasCols = (int)(altasWidth / spriteSize);
                int col = textureIndex % atlasCols;
                int row = textureIndex / atlasCols;

                // 해당 스프라이트의 uv 크기 (atlas 내 비율)
                float uvWidth = (float)spriteSize / altasWidth;
                float uvHeight = (float)spriteSize / altasHeight;

                float uvX = col * uvWidth;
                float uvY = 1 - row * uvHeight;

                Vector3[] vertices = target.mesh.vertices;
                Vector2[] uvs = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    // vertices[i]의 x, y가 이미 0~1 범위라고 가정
                    float normalizedX = vertices[i].x;
                    float normalizedY = vertices[i].y;

                    // 변환 공식: sprite 영역의 시작 UV + (로컬 좌표 * sprite 영역의 UV 크기)
                    uvs[i] = new Vector2(uvX + normalizedX * uvWidth,
                                         uvY + normalizedY * uvHeight);
                }

                return uvs;
            }
            void SaveMeshAsset(ConcurrentDictionary<int, EditMapGridData> map, Mesh mesh, int sceneIndex, int gridKey, int render_layer, int index, bool makeNewInstance, bool optimizeMesh)
            {
                if (false == map.ContainsKey(gridKey))
                {
                    map.TryAdd(gridKey, new EditMapGridData(gridKey));
                }

                string assetName = $"MapRender_{sceneIndex}_G{gridKey}_L{render_layer}_{index}";
                map[gridKey].AddAssetFile(assetName);
                map[gridKey].AddMeshAsset(render_layer, assetName);

                var path = "Assets/Rcs/MapRender/" + assetName + ".asset";
                if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }

                var meshToSave = (true == makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
                if (true == optimizeMesh)
                {
                    MeshUtility.Optimize(meshToSave);
                }
                AssetDatabase.CreateAsset(meshToSave, path);

                // Addressable Assets에 등록
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                var group = settings.FindGroup(assetGroupName);
                if (null != group)
                {
                    // Addressable 에셋 생성
                    var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                    entry.SetAddress(assetName);

                    EditorUtility.SetDirty(settings);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                }
                else
                {
                    Debug.LogError("Addressable Asset Group not found.");
                    return;
                }

                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif