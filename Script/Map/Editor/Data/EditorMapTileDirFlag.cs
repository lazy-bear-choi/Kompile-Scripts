#if UNITY_EDITOR
namespace Script.Map.Editor.Data
{
    using System;
    /// <summary> [Editor Only] 맵 타일의 연결 방향을 나타내는 플래그 열거형 </summary>
    [Flags]
    public enum EditorMapTileDirFlag
    {
        NONE  = 0,
        UP    = 1 << 0,
        DOWN  = 1 << 1,
        LEFT  = 1 << 2,
        RIGHT = 1 << 3
    }
}
#endif