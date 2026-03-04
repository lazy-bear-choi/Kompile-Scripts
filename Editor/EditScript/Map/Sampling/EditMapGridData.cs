#if UNITY_EDITOR
namespace Script.Data
{
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    public class EditMapGridData
    {
        public int gridKey;
        public ConcurrentDictionary<int, EditMapTileData> Data;
        public List<string> assetFiles;
        public UnityEngine.GameObject gameObject;
        public List<MapGridLayerData> LayerMeshAssets = new List<MapGridLayerData>();

        public ConcurrentDictionary<int, MapTileData> ParseData()
        {
            ConcurrentDictionary<int, MapTileData> data = new ConcurrentDictionary<int, MapTileData>();

            foreach (var kvp in Data)
            {
                data.TryAdd(kvp.Key, new MapTileData(kvp.Value));
            }

            return data;
        }
        public EditMapGridData(int targetGridKey)
        {
            gridKey = targetGridKey;
            Data = new ConcurrentDictionary<int, EditMapTileData>();
            assetFiles = new List<string>();
        }
        public void AddAssetFile(string fileName)
        {
            assetFiles.Add(fileName);
        }
        public bool TryAdd(int key, EditMapTileData navData)
        {
            return Data.TryAdd(key, navData);
        }
        public void AddMeshAsset(int layer, string fileName)
        {
            for (int i = 0; i < LayerMeshAssets.Count; ++i)
            {
                if (layer == LayerMeshAssets[i].layer)
                {
                    LayerMeshAssets[i].Add(fileName);
                    return;
                }
            }

            LayerMeshAssets.Add(new MapGridLayerData(layer, fileName));
        }
    }
}
#endif