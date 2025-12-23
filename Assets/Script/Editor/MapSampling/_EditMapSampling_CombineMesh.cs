#if UNITY_EDITOR
using Script.Data;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Experimental.AI;

public partial class EditMapSampling
{
    /// <summary>
    /// 복잡한 데이터 처리 공정(Pipeline)에서 발생하는 여러 상태 값과 설정 정보를
    /// 하나로 묶어 관리하는 '상태 저장소'이자 '전달 매개체'
    /// </summary>
    private class BakeContext
    {
        public int SceneIndex;
        public ConcurrentDictionary<int, EditMapGridData> Map;
        public List<(string path, string assetName)> CreatedAssets;
        public string AddressableGroupName;

        public BakeContext()
        {
            SceneIndex = 0;
            Map = null;
            CreatedAssets = new List<(string path, string assetName)>();
            AddressableGroupName = null;
        }
        public void Setup(int sceneIndex, ConcurrentDictionary<int, EditMapGridData> map, string groupName)
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
            UVs = null;
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
    }
    private struct GroupKey : IEquatable<GroupKey> // 박싱 방지를 위하여 IEquatable 구현
    {
        public readonly int RenderLayer;
        public readonly int GridKey;
        public GroupKey(int layer, int gKey)
        {
            RenderLayer = layer;
            GridKey = gKey;
        }
        public bool Equals(GroupKey other)
        {
            return RenderLayer == other.RenderLayer
                && GridKey == other.GridKey;
        }
        public override bool Equals(object obj)
        {
            if (obj is not GroupKey other)
            {
                return false;
            }

            return Equals(other);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                // 소수(397 등)를 사용하여 해시 충돌을 방지하는 일반적인 패턴
                int hash = 17;
                hash = (hash * 397) ^ RenderLayer.GetHashCode();
                hash = (hash * 397) ^ GridKey.GetHashCode();
                return hash;
            }

            /* unchecked
             * C#에서 정수 연산(덧셈, 곱셈 등)을 할 때 결과값이 자료형의 최대 범위를 넘어가면 **오버플로(Overflow)**가 발생합니다.
             * 오버플로가 발생해도 예외를 던지지 않고, 넘친 비트를 버린 채 **순환(Wrap-around)**된 값을 그대로 사용합니다.
             * 해시코드는 고유한 숫자를 만들기 위해 보통 여러 필드에 소수를 곱하고 더하는 복잡한 산술 연산을 수행합니다. 
             * 이 과정에서 int.MaxValue를 넘기는 경우가 매우 빈번한데, 해시코드 계산은 단순히 "구분 가능한 숫자"를 만드는 것이 목적이므로 값이 넘치더라도 
             * 예외 없이 계산을 완료하는 것이 성능과 안정성 측면에서 훨씬 유리하기 때문입니다.
             */
        }
    }

    private const int VERTEX_LIMIT = 65536;
    private const int BATCH_TILE_LIMIT = 512;
    private const int BATCH_VERTEX_TARGET = 200000;
    private const float SPRITE_SIZE = 256f;
    private const int ATLAS_WIDTH = 2048;
    private const int ATLAS_HEIGHT = 2048;
    private const string SAVE_PATH_ROOT = "Assets/Rcs/MapRender";

    private static readonly string PROGRESS_BAR_TITLE = "Bake Map - Combining Meshes";

    // Pooling Objects
    private static BakeContext cachedContext;
    private static readonly Stack<GroupAccumulator> accmPool = new Stack<GroupAccumulator>();
    private static readonly Stack<TileChunk> chunkPool = new Stack<TileChunk>();

    public static void CombineAndRegister(ConcurrentDictionary<int, EditMapGridData> map,
                                          EditMapTileObject[] tiles,
                                          int sceneIndex,
                                          string adderessableGroupName)
    {
        if (null == tiles || 0 == tiles.Length)
        {
            Debug.LogWarning("No tiles to process;");
            return;
        }

        if (false == AssetDatabase.IsValidFolder(SAVE_PATH_ROOT))
        {
            System.IO.Directory.CreateDirectory(SAVE_PATH_ROOT);
        }

        if (null == cachedContext)
        {
            cachedContext = new BakeContext();
        }
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
                if (true == EditorUtility.DisplayCancelableProgressBar(PROGRESS_BAR_TITLE,
                                                                        $"Processing {start}/{totalTiles}",
                                                                        (float)start / totalTiles))
                {
                    userCancelled = true;
                    break;
                }

                batchIndices.Clear();
                int currentBatchVertex = 0;
                int idx = start;

                // batch로 묶을 tile index를 batch_indices에 저장하는건가본데?
                while (idx < totalTiles
                       && batchIndices.Count < BATCH_TILE_LIMIT)
                {
                    EditMapTileObject tile = tiles[idx];
                    int vc = 0;
                    if (true == tile.TryGetSharedMesh(out Mesh tileMesh))
                    {
                        vc = tileMesh.vertexCount;
                    }
                    //int vc = (t?.MeshFilter?.sharedMesh != null) ? t.MeshFilter.sharedMesh.vertexCount : 0;

                    if (BATCH_VERTEX_TARGET < currentBatchVertex + vc
                        && 0 < batchIndices.Count)
                    {
                        break;
                    }

                    batchIndices.Add(idx);
                    currentBatchVertex += vc;
                    ++idx;
                }

                start += batchIndices.Count;
                ProcessBatch(cachedContext, tiles, batchIndices, accumulators);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 잔여 데이터 Flush -> Accumulator를 풀에 반환
        foreach (var kv in accumulators)
        {
            GroupKey key = kv.Key;
            GroupAccumulator accm = kv.Value;

            while (0 < accm.Tiles.Count)
            {
                FlushAccumulatorPart(cachedContext, key, accm);
            }
        }
        accumulators.Clear();

        RegisterAddressables(cachedContext);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(true == userCancelled ? "Bake cancelled by user" : "Bake Completed successfully");
    }

    private static void ProcessBatch(BakeContext ctx, EditMapTileObject[] tilesInGrid,
        List<int> indices, Dictionary<GroupKey, GroupAccumulator> accums)
    {
        EditMapTileObject tile;
        foreach (int i in indices)
        {
            tile = tilesInGrid[i];
            if (false == tile.TryGetSharedMesh(out Mesh mesh))
            {
                continue;
            }

            int vc = mesh.vertexCount;
            if (0 == vc)
            {
                continue;
            }

            // use Chunk Pooling
            TileChunk chunk = (0 < chunkPool.Count) ? chunkPool.Pop() : new TileChunk();
            chunk.Instance = new CombineInstance
            {
                mesh = mesh,
                transform = tile.transform.localToWorldMatrix
            };
            chunk.UVs = CalculateAtlasUVs(mesh, tile.TextureIndex);
            chunk.VertexCount = vc;
            chunk.RenderLayer = tile.RenderLayer;
            chunk.GridKey = tile.GridKey;

            // use Accumulator Pooling
            GroupKey key = new GroupKey(tile.RenderLayer, tile.GridKey);
            if (false == accums.TryGetValue(key, out GroupAccumulator acc))
            {
                acc = 0 < accmPool.Count ? accmPool.Pop() : new GroupAccumulator();
                accums[key] = acc;
            }
            acc.Tiles.Enqueue(chunk);
            acc.VertexSum += vc;

            while (acc.VertexSum > VERTEX_LIMIT)
            {
                FlushAccumulatorPart(ctx, key, acc);
            }
        }
    }

    /// <summary>
    /// 만약 4x4 칸으로 나뉜 아틀라스에서 첫 번째 칸의 이미지를 쓰고 싶다면, 단순히 그 이미지의 픽셀 좌표를 주는 것이 아니라 
    /// "전체 아틀라스 대비 가로 0~0.25, 세로 0.75~1.0 영역을 사용해라"라고 알려줘야 합니다. 
    /// 이 변환 과정을 수행하는 것이 CalculateAtlasUVs()입니다...
    /// UV 개념 아직 잘 모르니까 학습 요망: https://youtu.be/Yx2JNbv8Kpg?si=b4fPAq71ckiYXBd3
    /// </summary>
    private static Vector2[] CalculateAtlasUVs(Mesh mesh, int textureIndex)
    {
        Vector3[] verts = mesh.vertices;
        Vector2[] uvs = new Vector2[verts.Length];

        int atlasCols = ATLAS_WIDTH / ATLAS_HEIGHT;
        float uvW = SPRITE_SIZE / ATLAS_WIDTH;
        float uvH = SPRITE_SIZE / ATLAS_HEIGHT;

        float baseX = (textureIndex % atlasCols) * uvW;
        float baseY = (textureIndex / atlasCols) * uvH;

        float x, y;
        for (int i = 0; i < verts.Length; ++i)
        {
            x = baseX + verts[i].x * uvW;
            y = baseY + verts[i].y * uvH;
            uvs[i] = new Vector2(x, y);
        }

        return uvs;
    }
    private static void FlushAccumulatorPart(BakeContext ctx, GroupKey key, GroupAccumulator acc)
    {
        if (0 == acc.Tiles.Count)
        {
            return;
        }

        List<CombineInstance> takeInstances = new List<CombineInstance>();
        List<Vector2> takeUVs = new List<Vector2>();
        int takenVerts = 0;
        int tilesConsumed = 0;

        // TileChunk는 맵을 쪼개서 관리하기 위해, GroupAccumulator는 쪼개진 맵 안에서 같은 것끼리 뭉쳐서 그리기 위해 존재한다.
        // 이번 Part(mesh file)에 담을 수 있는 분량 만큼만 선별한다.
        foreach (TileChunk chunk in acc.Tiles)
        {
            // 다음 타일 추가 시 정점 제한을 넘으면 중단
            if (0 < takenVerts
                && VERTEX_LIMIT < takenVerts + chunk.VertexCount)
            {
                break;
            }

            takeInstances.Add(chunk.Instance);
            takeUVs.AddRange(chunk.UVs);
            takenVerts += chunk.VertexCount;
            ++tilesConsumed;

            if (VERTEX_LIMIT < takenVerts)
            {
                break;
            }
        }

        // 물리적 에셋 저장
        SaveMeshAsset(ctx, key, acc.PartIndex, takeInstances.ToArray(), takeUVs.ToArray());

        // 처리 완료된 데이터를 큐에서 제거 + 풀에 반납
        for (int i = 0; i < tilesConsumed; ++i)
        {
            var removed = acc.Tiles.Dequeue();
            acc.VertexSum -= removed.VertexCount;

            // Chunk 개체 초기화 + 풀에 반납하여 메모리 재사용
            removed.Clear();
            chunkPool.Push(removed);
        }

        // 파트 번호 증가 (파일명 중복 방지)
        ++acc.PartIndex;
    }
    private static void SaveMeshAsset(BakeContext ctx, GroupKey key, int partIdx, CombineInstance[] instances, Vector2[] uvs)
    {
        string assetName = $"MapRender_{ctx.SceneIndex}_G{key.GridKey}_L{key.RenderLayer}_{partIdx}";
        string path = $"{SAVE_PATH_ROOT}/{assetName}.asset";

        Mesh combinedMesh = new Mesh();
        try
        {
            if (VERTEX_LIMIT < uvs.Length)
            {
                combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            combinedMesh.CombineMeshes(instances, true, true);
            combinedMesh.uv = uvs;

            MeshUtility.Optimize(combinedMesh);

            if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(combinedMesh, path);

            if (null != ctx.Map)
            {
                EditMapGridData gridData = ctx.Map.GetOrAdd(key.GridKey, k => new EditMapGridData(k));
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
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (null == settings
            || 0 == ctx.CreatedAssets.Count)
        {
            return;
        }

        AddressableAssetGroup group = settings.FindGroup(ctx.AddressableGroupName);
        if (null == group)
        {
            return;
        }

        List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
        foreach ((string path, string assetName) in ctx.CreatedAssets)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (true == string.IsNullOrEmpty(guid))
            {
                continue;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.SetAddress(assetName);
            entries.Add(entry);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved,
                          entries.ToArray(),
                          true);
    }
}
#endif