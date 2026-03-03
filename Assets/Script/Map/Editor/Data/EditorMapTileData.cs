namespace Script.Map.Editor.Data
{
    using Unity.Burst;
    using UnityEngine;
    using Script.Map.Runtime;
    using Script.Map.Editor.Utility;

    /// <summary> 에디터 환경에서 베이킹 중 사용되는 타일 데이터 구조체 </summary>
    [BurstCompile]
    public struct EditorMapTileData
    {
        public long ID;
        public long NaviMask;
        public ushort LinkMask;
        public ushort RenderIndex;

        public readonly bool TryGetVerticeHeight(int vertice, out int height1000x)
        {
            ulong mask = (ulong)(NaviMask >> MapSamplingData.GetShiftOffset(vertice));
            mask &= MapSamplingData.HEIGHT_MASK;

            if (MapSamplingData.VERTEX_MISSING_FLAG == mask)
            {
                height1000x = default;
                return false;
            }

            float pivotY = EditorMapUtil.ComputeWorldPosition(ID).y;
            height1000x = Mathf.RoundToInt(1000 * (pivotY + MapSamplingData.CalculateRealHeight(mask)));
            return true;
        }
    }
}