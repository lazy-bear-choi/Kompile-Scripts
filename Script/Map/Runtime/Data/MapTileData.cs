namespace Script.Map.Runtime
{
    using MessagePack;
    using Unity.Collections;
#if UNITY_EDITOR
    using Script.Map.Editor.Data;
#endif
    /// <summary> 
    /// 단일 맵 타일의 길찾기 마스크(NaviMask)와 연결 마스크(LinkMask) 데이터를 정의하는 구조체 
    /// </summary>
    [MessagePackObject]
    public struct MapTileData
    {
        [ReadOnly, Key(0)] public long NaviMask;
        [ReadOnly, Key(1)] public ushort LinkMask;
        [ReadOnly, Key(2)] public ushort RenderIndex;

#if UNITY_EDITOR
        public MapTileData(EditorMapTileData edited)
        {
            NaviMask = edited.NaviMask;
            LinkMask = edited.LinkMask;
            RenderIndex = edited.RenderIndex;
        }
#endif
    }

}