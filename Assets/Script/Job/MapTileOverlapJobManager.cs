namespace Script.Manager
{
    using Script.Data;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;
    
    public class MapTileOverlapJobManager
    {
        private static MapTileOverlapJobManager instance;
        public static MapTileOverlapJobManager Instance => instance;

        private NativeArray<IngameMapTileData>  ingameMapTileDatas;

        public MapTileOverlapJobManager()
        {
            instance = this;

            int target_count = 4;
            ingameMapTileDatas = new NativeArray<IngameMapTileData>(target_count, Allocator.Persistent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <param name="tile">언제나 4개씩 들어온다. 하나라도 없으면 함수 호출을 하지 않는다.</param>
        public bool CheckMapTileMovable(Vector3 position, bool isSmall, float radius, params IngameMapTileData[] tiles)
        {
            int length = tiles.Length;
            for (int i = 0; i < length; ++i)
            {
                ingameMapTileDatas[i] = tiles[i];
            }

            MapTileMovableJob job = new MapTileMovableJob
            {
                IngameMapTileData   = ingameMapTileDatas,
                SphereCenter        = new float3(position.x, position.y, position.z),
                SphereRadius        = radius
            };

            bool isMovable = true;
            for (int i = 0; i < job.IngameMapTileData.Length; ++i)
            {
                if (false == job.Execute(i))
                {
                    isMovable = false;
                    break;
                }
            }

            return isMovable;
        }

        ~MapTileOverlapJobManager()
        {
            // DisposeNativeArrays()
            if (ingameMapTileDatas.IsCreated)
            { 
                ingameMapTileDatas.Dispose();
            }
        }
    }
}
