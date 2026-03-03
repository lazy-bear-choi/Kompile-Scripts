namespace Script.Map.Runtime
{
    using MessagePack;
    using Unity.Collections;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    /// <summary> 그리드 전체 타일(64*64)과 레이어 정보를 담는 최상위 런타임 데이터 컨테이너 </summary>
    [MessagePackObject]
    public class MapGridData
    {
        [Key(0), ReadOnly] public int Key;
        [Key(1), ReadOnly] public ConcurrentDictionary<int, MapTileData> NaviTileDict;
        [Key(2), ReadOnly] public List<MapGridLayerData> layerMeshAssets;

        public bool TryGetTileData(int tileIntKey, out MapTileData tileData)
        {
            return NaviTileDict.TryGetValue(tileIntKey, out tileData);
        }
    }

}