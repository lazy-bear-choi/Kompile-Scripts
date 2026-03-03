namespace Script.Map.Runtime
{
    using MessagePack;
    using System.Collections.Generic;
    using Unity.Collections;

    /// <summary> 맵 그리드 내의 특정 레이어에 배치된 메시 에셋 목록을 보관 </summary>
    [MessagePackObject]
    public class MapGridLayerData
    {
        [Key(0), ReadOnly] public int _layer;
        [Key(1), ReadOnly] public List<string> _assets;

        public MapGridLayerData() { }
        public MapGridLayerData(int _layer, string asset)
        {
            this._layer = _layer;
            _assets = new List<string>() { asset };
        }
        public void Add(string asset)
        {
            _assets.Add(asset);
        }
    }

}