#if UNITY_EDITOR
namespace Script.Map.Editor.Data
{
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using Script.Map.Runtime;

    /// <summary>
    /// 맵 베이킹(Baking) 과정에서 사용할 임시 데이터 구조체<br/>
    /// Engine에서 가공된 데이터를 들고 있다가, 최종적으로 저장될 형태로 파싱한다.
    /// </summary>
    public class EditorMapGridBakeData
    {
        public int gridKey;
        public ConcurrentDictionary<int, EditorMapTileData> Data;
        public List<string> assetFiles;
        public UnityEngine.GameObject gameObject;
        public List<MapGridLayerData> LayerMeshAssets = new List<MapGridLayerData>();

        public EditorMapGridBakeData(int targetGridKey)
        {
            gridKey = targetGridKey;
            Data = new ConcurrentDictionary<int, EditorMapTileData>();
            assetFiles = new List<string>();
        }

        public ConcurrentDictionary<int, MapTileData> ParseData()
        {
            var data = new ConcurrentDictionary<int, MapTileData>();

            foreach (var kvp in Data)
            {
                data.TryAdd(kvp.Key, new MapTileData(kvp.Value));
            }

            return data;
        }

        public void AddAssetFile(string fileName)
        {
            assetFiles.Add(fileName);
        }

        public void AddMeshAsset(int layer, string fileName)
        {
            for (int i = 0; i < LayerMeshAssets.Count; ++i)
            {
                if (LayerMeshAssets[i]._layer == layer)
                {
                    LayerMeshAssets[i].Add(fileName);
                }
            }

            LayerMeshAssets.Add(new MapGridLayerData(layer, fileName));
        }
    }
}
#endif