namespace Script.Data
{
    using MessagePack;
    using Unity.Collections;
    using System.Collections.Generic;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0), ReadOnly] 
        public int Key;

        [Key(1), ReadOnly] 
        public ConcurrentDictionary<int, MapTileData> NaviTileDict;

        [Key(2), ReadOnly] 
        public List<GridLayerData> layerMeshAssets;

        public bool TryGetTileData(int tileIntKey, out MapTileData tileData)
        {
            return NaviTileDict.TryGetValue(tileIntKey, out tileData);
        }
    }
}
