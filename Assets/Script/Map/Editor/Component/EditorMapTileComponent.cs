

namespace Script.Map.Editor
{
    using UnityEngine;

    /// <summary> [Editor Only]
    /// 개별 맵 타일 게임 오브젝트에 부착되어, 베이킹에 필요한 기초 데이터(레이어, 텍스처, 높이 마스크)를 보관
    /// </summary>
    public class EditorMapTileComponent : MonoBehaviour
    {
        [Tooltip("타일이 그려질 렌더 레이어 (예: 0=바닥, 1=위층)")]
        public ushort RenderLayer;

        [Tooltip("아틀라스에서 사용할 텍스처 인덱스")]
        public int TextureIndex;

        [Tooltip("타일의 13개 정점에 대한 높이 데이터 (ulong 비트마스크)")]
        public ulong HeightMask;
    }
}