namespace Script.Map.Runtime
{
    /// <summary> 맵 샘플링 및 길찾기에 사용되는 핵심 상수와 비트 마스크를 제공 </summary>
    public class MapSamplingData
    {
        // Core bit Masks & Values
        public const ulong  HEIGHT_MASK          = 0b_1111;
        public const ulong  VERTEX_MISSING_FLAG  = 0b_1111;
        public const int    MAX_HEIGHT_VALUE     = 8;
        public const float  HEIGHT_MULTIPLIER    = 0.125f;
        public const float  ENTITY_SEARCH_RADIUS = 0.35f;

        // Structure Limits
        public const int VERTEX_COUNT       = 13;   // 타일 1개의 총 정점 개수
        public const int SUBTILTE_COUNT     = 16;   // 타일 1개를 구성하는 작은 삼각형의 개수
        public const int VERTEX_PER_SUBTILE = 3;    // 삼각형 하나를 이루는 점의 개수

        // Data Helper Methods
        public static int GetShiftOffset(int vertexIndex)
        {
            return vertexIndex * 4;
        }
        public static float CalculateRealHeight(ulong bitFlagValue)
        {
            if (VERTEX_MISSING_FLAG == bitFlagValue)
            {
                return -1f;
            }

            return bitFlagValue * HEIGHT_MULTIPLIER;
        }
    }
}