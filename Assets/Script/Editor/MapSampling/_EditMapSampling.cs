#if UNITY_EDITOR
using Script.Data;
using Script.Editor.MapSampling;
using Script.Manager;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// partial: STUDY_EditMapSampling
/// partial: STUDY_EditMapSampling_combineMesh.cs
/// </summary>
public partial class EditMapSampling
{
    private class CombineMeshData
    {
        public List<CombineInstance> combineInstances;
        public List<Vector2> combinedUVs;
        public int vertexCount;
        public int index;
    }

    private readonly string MAP_NAVI_DATA_PATH = "Rcs\\Bytes\\MapNavi";

    // tile link를 할 때에 탐색 순서가 지정되어 있음!
    private readonly float[] DIFF_Y = new float[] { 0, 1, -1 };
    private readonly float2[] LINK_DIR = new float2[]
    {
            new float2(1, -1), new float2(1, 1), new float2(-1, 1), new float2(-1, -1),
            new float2(0, -1), new float2(1, 0), new float2(0, 1),  new float2(-1, 0)
    };

    private byte sceneIndex = 0;

    private ConcurrentDictionary<int, EditMapGridData> map;
    public ConcurrentDictionary<int, EditMapGridData> Map => map;

    public void Bake()
    {
        Debug.Log($"Start Bake Map");

        // ## set data
        var instance = Object.FindFirstObjectByType<EditMapTileSampling>();
        var instanceTransform = instance.MapRoot;
        sceneIndex = instance.SceneIndex;

        EditMapTileObject[] tiles = instanceTransform.GetComponentsInChildren<EditMapTileObject>(true);
        int length = tiles.Length;
        Allocator allocationType = Allocator.TempJob;

        var nativeSceneIndex = new NativeArray<byte>(length, allocationType);
        var nativeRenderLayer = new NativeArray<ushort>(length, allocationType);
        var nativePosition = new NativeArray<float3>(length, allocationType);
        var nativeRotateY = new NativeArray<float>(length, allocationType);
        var nativeHeights = new NativeArray<ulong>(length, allocationType);
        var nativeResult = new NativeArray<EditMapTileData>(length, allocationType);

        EditMapTileObject tileObject; // TODO: EditMapTileObject 식으로 이름 바꿔야겠네.
        for (int i = 0; i < tiles.Length; ++i)
        {
            tileObject = tiles[i];

            nativeSceneIndex[i] = sceneIndex;
            nativeRenderLayer[i] = tileObject.RenderLayer;

            int x = Mathf.FloorToInt(tileObject.transform.position.x);
            int y = Mathf.FloorToInt(tileObject.transform.position.y);
            int z = Mathf.FloorToInt(tileObject.transform.position.z);
            nativePosition[i] = new float3(x, y, z);

            nativeRotateY[i] = Mathf.FloorToInt(tileObject.transform.eulerAngles.y);
            nativeHeights[i] = tileObject.HeightMask;
        }

        EditMapTileJob job = new EditMapTileJob
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


        // ## Register Map
        ushort renderIndex;
        long naviMask;

        map = new ConcurrentDictionary<int, EditMapGridData>();
        for (int i = 0; i < nativeResult.Length; ++i)
        {
            EditMapUtil.ComputeKey(nativeResult[i].ID, out int gridKey, out int tileKey);
            naviMask = nativeResult[i].NaviMask;
            renderIndex = nativeResult[i].RenderIndex;

            if (false == map.ContainsKey(gridKey))
            {
                map.TryAdd(gridKey, new EditMapGridData(gridKey));
            }

            EditMapTileData tileData = new EditMapTileData()
            {
                ID = nativeResult[i].ID, //EditMapUtil.ComputeID(gridKey, tileKey)로 굳이 같은 계산을 할 필요가?
                NaviMask = naviMask,
                LinkMask = default,            // Link() 단계에서 값 입력
                RenderIndex = renderIndex
            };
            map[gridKey].TryAdd(tileKey, tileData);
        }

        long startID = nativeResult[0].ID;


        // ## Dispose NativeArray
        nativeSceneIndex.Dispose();
        nativeRenderLayer.Dispose();
        nativePosition.Dispose();
        nativeRotateY.Dispose();
        nativeHeights.Dispose();
        nativeResult.Dispose();


        // ## Set Grid Data
        LinkTiles(map);
        CombineAndRegister(map, tiles, sceneIndex, "MapRender");   // **Streaming, 부분 처리 방식으로

        // ## Save Data.bin
        foreach (KeyValuePair<int, EditMapGridData> grid in map)
        {
            MapGridData mapGridData = new MapGridData()
            {
                Key = grid.Key,
                NaviTileDict = grid.Value.ParseData(),
                layerMeshAssets = grid.Value.LayerMeshAssets
            };

            AssetManager.WriteBinaryFile<MapGridData>(
                data: mapGridData,
                dataPath: MAP_NAVI_DATA_PATH,
                fileName: $"MapNavi_{mapGridData.Key}",
                addressableGroup: "MapNavi"
                );
        }

        Debug.Log($"End Bake (length: {tiles.Length})");
        System.GC.Collect();
    }
    private void LinkTiles(ConcurrentDictionary<int, EditMapGridData> map)
    {
        List<EditMapTileData> allTiles = new List<EditMapTileData>();
        foreach (var grid in map.Values)
        {
            foreach (var tile in grid.Data.Values)
            {
                allTiles.Add(tile);
            }
        }

        int count = allTiles.Count;
        if (0 == count)
        {
            return;
        }

        var allocType = Allocator.TempJob;
        var keyArray = new NativeArray<long>(count, allocType);
        var tileMap = new NativeHashMap<long, EditMapTileData>(count, allocType);
        var linkDirs = new NativeArray<float2>(LINK_DIR, allocType);
        var diffYs = new NativeArray<float>(DIFF_Y, allocType);
        var jobResult = new NativeArray<EditMapTileData>(count, allocType);

        // init data
        for (int i = 0; i < count; ++i)
        {
            keyArray[i] = allTiles[i].ID;
            tileMap.TryAdd(allTiles[i].ID, allTiles[i]);
        }

        // schedule job
        EditLinkTilesJob linkJob = new EditLinkTilesJob
        {
            KeyArray = keyArray,
            Map = tileMap,
            Results = jobResult,
            LinkDirs = linkDirs,
            DiffYs = diffYs
        };
        JobHandle handle = linkJob.Schedule(count, 64);
        handle.Complete();

        // save to result
        for (int i = 0; i < count; ++i)
        {
            EditMapTileData resultTile = jobResult[i];
            EditMapUtil.ComputeKey(resultTile.ID, out int gKey, out int tKey);

            if (true == map.TryGetValue(gKey, out var gridData))
            {
                gridData.Data[tKey] = resultTile;
            }
        }

        // dispose native container
        keyArray.Dispose();
        tileMap.Dispose();
        linkDirs.Dispose();
        diffYs.Dispose();
        jobResult.Dispose();

        Debug.Log($"LinkTiles Job Completed: {count} tiles processed.");
    }
}
#endif