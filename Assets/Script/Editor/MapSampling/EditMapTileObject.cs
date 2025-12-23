#if UNITY_EDITOR
namespace Script.Data
{
    using Script.Index;
    using System;
    using UnityEditor;
    using UnityEngine;
    using static Script.Index.MapTileIndex;

    [Serializable]       // 에셋으로 저장하기 위함
    [ExecuteInEditMode]  // 에디터에서 텍스쳐 곧장 적용하기 위함
    public class EditMapTileObject : MonoBehaviour
    {
        private const int SPRITE_WIDTH  = 256;
        private const int SPRITE_HEIGHT = 256;

        [Header("Render")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private bool isOnlyRender;
        [SerializeField] private ushort renderLayer;
        [SerializeField] private TextureIndex textureType;

        [Header("Data")]
        [SerializeField] private ulong heightMask;
        
        public int GridKey => EditMapUtil.ComputeGridKey(transform.position);
        public MeshFilter MeshFilter => meshFilter;
        public ushort RenderLayer => renderLayer;
        public int TextureIndex => (int)textureType;
        public ulong HeightMask => heightMask;

        private void Awake()
        {
            // ExecuteInEditMode 라서 Edit Mode 에서도 호출된다.
            meshFilter = transform.GetComponent<MeshFilter>();
            meshRenderer = transform.GetComponent<MeshRenderer>();
        }

        /// <summary> 프리팹 데이터를 초기화 ( != 실제 맵 타일 오브젝트) <br/>
        /// heights, isSmall 데이터만 저장한다.
        /// </summary>
        public void InitializePrefab(int[] heights, bool isSmall)
        {
            int height;
            ulong heightFlag;
  
            for (int i = 0; i < heights.Length; ++i)
            {
                height      = heights[i];
                heightFlag  = (-1 == height) ? HEIGHT_MASK : (ulong)height;
                heightMask |= heightFlag << i * HEIGHT_BITS;
            }

            //this.isSmall = isSmall;
            EditorUtility.SetDirty(this);
        }

        public bool TryGetSharedMesh(out Mesh outSharedMesh)
        {
            if (null == meshFilter)
            {
                outSharedMesh = null;
                return false;
            }
            outSharedMesh = meshFilter.sharedMesh;
            return null != outSharedMesh;
        }

        private void OnValidate()
        {
            // ApplyTexture();

            // 공유된 Material 유지
            Texture texture = meshRenderer.sharedMaterial.mainTexture;
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            int columnIndex = (int)textureType % 8;
            int rowIndex = (int)textureType / 8;

            float uMin = columnIndex * (SPRITE_WIDTH / (float)textureWidth);
            float vMin = 1.0f - (rowIndex + 1) * (SPRITE_HEIGHT / (float)textureHeight);

            Vector2 uvOffset = new Vector2(uMin, vMin); // UV 시작 좌표
            Vector2 uvScale = new Vector2(SPRITE_WIDTH / (float)textureWidth, SPRITE_HEIGHT / (float)textureHeight); // 크기

            // MaterialPropertyBlock을 사용해 개별 속성 적용
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);

            //propertyBlock.SetColor("_Color", GetColorByEnum(textureType)); // 개별 색상 적용
            propertyBlock.SetVector("_UVOffset", uvOffset); // UV Offset 적용
            propertyBlock.SetVector("_UVScale", uvScale);   // UV Scale 적용

            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
#endif
