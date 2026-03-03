#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

using Script.GamePlay.Global.Editor.Utility; // EditorAssetUtil 네임스페이스
using Script.Map.Runtime;                    // 런타임 데이터 시스템
using Script.Map.Editor.Utility;             // EditorMapUtil 위치 (사용자 제공 코드 기반)
using Script.Map.Editor.Data;                // EditorMapGridBakeData 위치

namespace Script.Map.Editor
{
    /// <summary>
    /// [Framework: Engine] [Editor Only]
    /// 맵 베이킹(Baking) 프로세스를 총괄하는 구동기(Engine)입니다.
    /// 내부적으로 큐와 스택을 활용하여 메시 병합 및 데이터 처리를 조율합니다.
    /// (Zero-Centered Origin 좌표계를 완벽히 지원합니다)
    /// </summary>
    public class EditorMapBakeEngine
    {
        // =========================================================================
        // --- Engine Internal States (파편화되었던 클래스들을 엔진 내부로 캡슐화) ---
        // =========================================================================
        private class BakeContext
        {
            public int SceneIndex;
            public ConcurrentDictionary<int, EditorMapGridBakeData> Map;
            public List<(string path, string assetName)> CreatedAssets;
            public string AddressableGroupName;

            public BakeContext()
            {
                SceneIndex = 0;
                Map = null;
                CreatedAssets = new List<(string path, string assetName)>();
                AddressableGroupName = null;
            }
            public void Setup(int sceneIndex, ConcurrentDictionary<int, EditorMapGridBakeData> map, string groupName)
            {
                SceneIndex = sceneIndex;
                Map = map;
                AddressableGroupName = groupName;
                CreatedAssets.Clear();
            }
        }

        private class TileChunk
        {
            public CombineInstance Instance;
            public Vector2[] UVs;
            public int VertexCount;
            public int GridKey;
            public int RenderLayer;

            public void Clear()
            {
                Instance.mesh = null;
                Instance = default;
                UVs = null;
                VertexCount = 0;
                GridKey = 0;
                RenderLayer = 0;
            }
        }

        private class GroupAccumulator
        {
            public Queue<TileChunk> Tiles;
            public int VertexSum = 0;
            public int PartIndex = 0;

            public GroupAccumulator()
            {
                Tiles = new Queue<TileChunk>();
                VertexSum = 0;
                PartIndex = 0;
            }

            public void Clear()
            {
                Tiles.Clear();
                VertexSum = 0;
                PartIndex = 0;
            }
        }

        private readonly struct GroupKey : IEquatable<GroupKey>
        {
            public readonly int RenderLayer;
            public readonly int GridKey;
            public GroupKey(int layer, int gKey)
            {
                RenderLayer = layer;
                GridKey = gKey;
            }

            public bool Equals(GroupKey other) => RenderLayer == other.RenderLayer && GridKey == other.GridKey;
            public override bool Equals(object obj) => obj is GroupKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 397) ^ RenderLayer.GetHashCode();
                    hash = (hash * 397) ^ GridKey.GetHashCode();
                    return hash;
                }
            }
        }
        // =========================================================================

        private readonly string MAP_NAVI_DATA_PATH = "Resources/MapNavi"; // 에셋 시스템 규격에 맞춰 경로 단순화
        private readonly float[] DIFF_Y = new float[] { 0, 1, -1 };
        private readonly float2[] LINK_DIR = new float2[]
        {
            new float2(1, -1), new float2(1, 1), new float2(-1, 1), new float2(-1, -1),
            new float2(0, -1), new float2(1, 0), new float2(0, 1),  new float2(-1, 0)
        };

        private const int VERTEX_LIMIT = 65536;
        private const int BATCH_TILE_LIMIT = 512;
        private const int BATCH_VERTEX_TARGET = 200000;
        private const float SPRITE_SIZE = 256f;
        private const int ATLAS_WIDTH = 2048;
        private const int ATLAS_HEIGHT = 2048;
        private const string SAVE_PATH_ROOT = "Assets/Resources/MapRender";
        private static readonly string PROGRESS_BAR_TITLE = "Bake Map - Combining Meshes";

        private static BakeContext cachedContext;
        private static readonly Stack<GroupAccumulator> accmPool = new Stack<GroupAccumulator>();
        private static readonly Stack<TileChunk> chunkPool = new Stack<TileChunk>();

        private byte sceneIndex = 0;
        private ConcurrentDictionary<int, EditorMapGridBakeData> map;

        public void Bake()
        {
            Debug.Log($"[EditorMapBakeEngine] Start Bake Map");

            var instance = Object.FindFirstObjectByType<EditorMapSamplingComponent>();
            if (instance == null)
            {
                Debug.LogError("EditorMapSamplingComponent를 찾을 수 없습니다!");
                return;
            }
            var instanceTransform = instance.transform;
            sceneIndex = instance.SceneIndex;

            var tiles = instanceTransform.GetComponentsInChildren<EditorMapTileComponent>(true);
            int length = tiles.Length;
            Allocator allocationType = Allocator.TempJob;

            var nativeSceneIndex = new NativeArray<byte>(length, allocationType);
            var nativeRenderLayer = new NativeArray<ushort>(length, allocationType);
            var nativePosition = new NativeArray<float3>(length, allocationType);
            var nativeRotateY = new NativeArray<float>(length, allocationType);
            var nativeHeights = new NativeArray<ulong>(length, allocationType);
            var nativeResult = new NativeArray<EditorMapTileData>(length, allocationType);

            for (int i = 0; i < tiles.Length; ++i)
            {
                var tileObject = tiles[i];

                nativeSceneIndex[i] = sceneIndex;
                nativeRenderLayer[i] = tileObject.RenderLayer;

                int x = Mathf.FloorToInt(tileObject.transform.position.x);
                int y = Mathf.FloorToInt(tileObject.transform.position.y);
                int z = Mathf.FloorToInt(tileObject.transform.position.z);
                nativePosition[i] = new float3(x, y, z);

                nativeRotateY[i] = Mathf.FloorToInt(tileObject.transform.eulerAngles.y);
                nativeHeights[i] = tileObject.HeightMask;
            }

            EditorMapTileBakeJob job = new EditorMapTileBakeJob
            {
                SceneIndex = nativeSceneIndex,
                RenderLayer = nativeRenderLayer,
                Position = nativePosition,
                RotY = nativeRotateY,
                Height = nativeHeights,
                Data = nativeResult
            };
            JobHandle jobHandle = job.Schedule(tiles.Length, 64);
            jobHandle.Complete();

            ushort renderIndex;
            long naviMask;
            int[] computedGridKeys = new int[length];

            map = new ConcurrentDictionary<int, EditorMapGridBakeData>();
            for (int i = 0; i < nativeResult.Length; ++i)
            {
                // [Kompile] Zero-Centered 기반 ComputeKey 호출 (음수 완벽 지원)
                EditorMapUtil.ComputeKey(nativeResult[i].ID, out int gridKey, out int tileKey);
                naviMask = nativeResult[i].NaviMask;
                renderIndex = nativeResult[i].RenderIndex;

                computedGridKeys[i] = gridKey;

                if (!map.ContainsKey(gridKey))
                {
                    map.TryAdd(gridKey, new EditorMapGridBakeData(gridKey));
                }

                EditorMapTileData tileData = new EditorMapTileData()
                {
                    ID = nativeResult[i].ID,
                    NaviMask = naviMask,
                    LinkMask = default,
                    RenderIndex = renderIndex
                };

                // [수정] TryAdd 함수 부재 대응 -> ConcurrentDictionary의 인덱서 또는 TryAdd 사용
                // grid.Data가 ConcurrentDictionary이므로 기본 TryAdd 사용 가능
                map[gridKey].Data.TryAdd(tileKey, tileData);
            }

            nativeSceneIndex.Dispose();
            nativeRenderLayer.Dispose();
            nativePosition.Dispose();
            nativeRotateY.Dispose();
            nativeHeights.Dispose();
            nativeResult.Dispose();

            LinkTiles(map);
            CombineAndRegister(map, tiles, computedGridKeys, sceneIndex, "MapNavi");

            // 최종 MessagePack 데이터 파일 저장
            foreach (KeyValuePair<int, EditorMapGridBakeData> grid in map)
            {
                MapGridData mapGridData = new MapGridData()
                {
                    Key = grid.Key,
                    NaviTileDict = grid.Value.ParseData(),
                    layerMeshAssets = grid.Value.LayerMeshAssets
                };

                // [Kompile] 전역 파일 시스템(EditorAssetUtil) 사용
                EditorAssetUtil.WriteBinaryFile<MapGridData>(
                    data: mapGridData,
                    relativePath: MAP_NAVI_DATA_PATH,
                    fileName: $"MapNavi_{mapGridData.Key}",
                    addressableGroup: "MapNavi",
                    addressableLabel: "MapNavi"
                );
            }

            Debug.Log($"[EditorMapBakeEngine] End Bake (length: {tiles.Length})");
            System.GC.Collect();
        }

        private void LinkTiles(ConcurrentDictionary<int, EditorMapGridBakeData> map)
        {
            List<EditorMapTileData> allTiles = new List<EditorMapTileData>();
            foreach (var grid in map.Values)
            {
                foreach (var tile in grid.Data.Values)
                {
                    allTiles.Add(tile);
                }
            }

            int count = allTiles.Count;
            if (0 == count) return;

            var allocType = Allocator.TempJob;
            var keyArray = new NativeArray<long>(count, allocType);
            var tileMap = new NativeHashMap<long, EditorMapTileData>(count, allocType);
            var linkDirs = new NativeArray<float2>(LINK_DIR, allocType);
            var diffYs = new NativeArray<float>(DIFF_Y, allocType);
            var jobResult = new NativeArray<EditorMapTileData>(count, allocType);

            for (int i = 0; i < count; ++i)
            {
                keyArray[i] = allTiles[i].ID;
                tileMap.TryAdd(allTiles[i].ID, allTiles[i]);
            }

            EditorMapLinkBakeJob linkJob = new EditorMapLinkBakeJob
            {
                KeyArray = keyArray,
                Map = tileMap,
                Results = jobResult,
                LinkDirs = linkDirs,
                DiffYs = diffYs
            };
            JobHandle handle = linkJob.Schedule(count, 64);
            handle.Complete();

            for (int i = 0; i < count; ++i)
            {
                EditorMapTileData resultTile = jobResult[i];
                EditorMapUtil.ComputeKey(resultTile.ID, out int gKey, out int tKey);

                if (map.TryGetValue(gKey, out var gridData))
                {
                    gridData.Data[tKey] = resultTile;
                }
            }

            keyArray.Dispose();
            tileMap.Dispose();
            linkDirs.Dispose();
            diffYs.Dispose();
            jobResult.Dispose();
        }

        private static void CombineAndRegister(ConcurrentDictionary<int, EditorMapGridBakeData> map,
                                              EditorMapTileComponent[] tiles,
                                              int[] gridKeys,
                                              int sceneIndex,
                                              string adderessableGroupName)
        {
            if (null == tiles || 0 == tiles.Length) return;

            if (AssetDatabase.IsValidFolder(SAVE_PATH_ROOT)) AssetDatabase.DeleteAsset(SAVE_PATH_ROOT);
            if (!System.IO.Directory.Exists(SAVE_PATH_ROOT)) System.IO.Directory.CreateDirectory(SAVE_PATH_ROOT);
            AssetDatabase.Refresh();

            if (null == cachedContext) cachedContext = new BakeContext();
            cachedContext.Setup(sceneIndex, map, adderessableGroupName);

            var accumulators = new Dictionary<GroupKey, GroupAccumulator>();
            int totalTiles = tiles.Length;
            bool userCancelled = false;

            try
            {
                int start = 0;
                List<int> batchIndices = new List<int>();
                while (start < totalTiles)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(PROGRESS_BAR_TITLE, $"Processing {start}/{totalTiles}", (float)start / totalTiles))
                    {
                        userCancelled = true;
                        break;
                    }

                    batchIndices.Clear();
                    int currentBatchVertex = 0;
                    int idx = start;

                    while (idx < totalTiles && batchIndices.Count < BATCH_TILE_LIMIT)
                    {
                        var tile = tiles[idx];
                        int vc = 0;

                        // [수정] TryGetSharedMesh 함수가 Component에 없으므로 MeshFilter 컴포넌트를 직접 추출하여 사용
                        MeshFilter meshFilter = tile.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            vc = meshFilter.sharedMesh.vertexCount;
                        }

                        if (BATCH_VERTEX_TARGET < currentBatchVertex + vc && 0 < batchIndices.Count) break;

                        batchIndices.Add(idx);
                        currentBatchVertex += vc;
                        ++idx;
                    }

                    start += batchIndices.Count;
                    ProcessBatch(cachedContext, tiles, gridKeys, batchIndices, accumulators);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (var kv in accumulators)
            {
                GroupKey key = kv.Key;
                GroupAccumulator accm = kv.Value;

                while (0 < accm.Tiles.Count) FlushAccumulatorPart(cachedContext, key, accm);

                accm.Clear();
                accmPool.Push(accm);
            }
            accumulators.Clear();

            // 메시 에셋 저장이 완료되었으므로, 해당 에셋들을 Addressables 시스템에 등록
            RegisterAddressables(cachedContext);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(userCancelled ? "Bake cancelled by user" : "Bake Completed successfully");
        }

        private static void ProcessBatch(BakeContext ctx, EditorMapTileComponent[] tilesInGrid, int[] gridKeys,
            List<int> indices, Dictionary<GroupKey, GroupAccumulator> accums)
        {
            foreach (int i in indices)
            {
                var tile = tilesInGrid[i];

                // [수정] MeshFilter에서 직접 Mesh 가져오기
                MeshFilter mf = tile.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                Mesh mesh = mf.sharedMesh;

                int vc = mesh.vertexCount;
                if (0 == vc) continue;

                int correctGridKey = gridKeys[i];

                TileChunk chunk = (0 < chunkPool.Count) ? chunkPool.Pop() : new TileChunk();
                chunk.Instance = new CombineInstance
                {
                    mesh = mesh,
                    transform = tile.transform.localToWorldMatrix
                };
                chunk.UVs = CalculateAtlasUVs(mesh, tile.TextureIndex);
                chunk.VertexCount = vc;
                chunk.RenderLayer = tile.RenderLayer;
                chunk.GridKey = correctGridKey;

                GroupKey key = new GroupKey(tile.RenderLayer, correctGridKey);
                if (!accums.TryGetValue(key, out GroupAccumulator acc))
                {
                    acc = 0 < accmPool.Count ? accmPool.Pop() : new GroupAccumulator();
                    acc.Clear();
                    accums[key] = acc;
                }
                acc.Tiles.Enqueue(chunk);
                acc.VertexSum += vc;

                while (acc.VertexSum > VERTEX_LIMIT) FlushAccumulatorPart(ctx, key, acc);
            }
        }

        private static Vector2[] CalculateAtlasUVs(Mesh mesh, int textureIndex)
        {
            Vector3[] verts = mesh.vertices;
            Vector2[] uvs = new Vector2[verts.Length];

            int atlasCols = ATLAS_WIDTH / ATLAS_HEIGHT;
            float uvW = SPRITE_SIZE / ATLAS_WIDTH;
            float uvH = SPRITE_SIZE / ATLAS_HEIGHT;

            float baseX = (textureIndex % atlasCols) * uvW;
            float baseY = (textureIndex / atlasCols) * uvH;

            for (int i = 0; i < verts.Length; ++i)
            {
                uvs[i] = new Vector2(baseX + verts[i].x * uvW, baseY + verts[i].y * uvH);
            }

            return uvs;
        }

        private static void FlushAccumulatorPart(BakeContext ctx, GroupKey key, GroupAccumulator acc)
        {
            if (0 == acc.Tiles.Count) return;

            List<CombineInstance> takeInstances = new List<CombineInstance>();
            List<Vector2> takeUVs = new List<Vector2>();
            int takenVerts = 0;
            int tilesConsumed = 0;

            foreach (TileChunk chunk in acc.Tiles)
            {
                if (0 < takenVerts && VERTEX_LIMIT < takenVerts + chunk.VertexCount) break;

                takeInstances.Add(chunk.Instance);
                takeUVs.AddRange(chunk.UVs);
                takenVerts += chunk.VertexCount;
                ++tilesConsumed;

                if (VERTEX_LIMIT < takenVerts) break;
            }

            SaveMeshAsset(ctx, key, acc.PartIndex, takeInstances.ToArray(), takeUVs.ToArray());

            for (int i = 0; i < tilesConsumed; ++i)
            {
                var removed = acc.Tiles.Dequeue();
                acc.VertexSum -= removed.VertexCount;
                removed.Clear();
                chunkPool.Push(removed);
            }

            ++acc.PartIndex;
        }

        private static void SaveMeshAsset(BakeContext ctx, GroupKey key, int partIdx, CombineInstance[] instances, Vector2[] uvs)
        {
            string assetName = $"MapRender_{ctx.SceneIndex}_G{key.GridKey}_L{key.RenderLayer}_{partIdx}";
            string path = $"{SAVE_PATH_ROOT}/{assetName}.asset";

            Mesh combinedMesh = new Mesh();
            try
            {
                if (VERTEX_LIMIT < uvs.Length) combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                combinedMesh.CombineMeshes(instances, true, true);
                combinedMesh.uv = uvs;
                MeshUtility.Optimize(combinedMesh);

                if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(combinedMesh, path);

                if (null != ctx.Map)
                {
                    EditorMapGridBakeData gridData = ctx.Map.GetOrAdd(key.GridKey, k => new EditorMapGridBakeData(k));
                    gridData.AddAssetFile(assetName);
                    gridData.AddMeshAsset(key.RenderLayer, assetName);
                }

                ctx.CreatedAssets.Add((path, assetName));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save mesh {assetName}: {e.Message}");
            }
        }

        private static void RegisterAddressables(BakeContext ctx)
        {
            // UnityEditor.AddressableAssets 네임스페이스 필요
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (null == settings || 0 == ctx.CreatedAssets.Count) return;

            UnityEditor.AddressableAssets.Settings.AddressableAssetGroup group = settings.FindGroup(ctx.AddressableGroupName);
            if (null == group) return;

            List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> entries = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>();
            foreach ((string path, string assetName) in ctx.CreatedAssets)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (true == string.IsNullOrEmpty(guid)) continue;

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.SetAddress(assetName);
                entries.Add(entry);
            }

            settings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, entries.ToArray(), true);
        }
    }
}
#endif