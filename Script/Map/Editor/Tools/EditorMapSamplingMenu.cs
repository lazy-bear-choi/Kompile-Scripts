#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using MessagePack;
    using MessagePack.Resolvers;
    using Script.Map.Runtime;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.AddressableAssets;


    /// <summary>
    /// [Editor Only] 에디터 상단 메뉴의 진입점을 담당 => EditorMapBakeEngine 생성 및 실행
    /// </summary>
    public class EditorMapSamplingMenu
    {
        [MenuItem("Tools/MapSampling/Bake Path Tiles to .bytes")]
        public static void Bake()
        {
            EditorMapBakeEngine engine = new EditorMapBakeEngine();
            engine.Bake();
        }

        [MenuItem("Tools/MapSampling/Load Baked Map Tiles")]
        public static async void EditLoadAll()
        {
            string label = "MapNavi";
            var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
            {
                if (null != textAsset)
                {
                    var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                    MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(textAsset.bytes, options);
                    Debug.Log($"[Load Baked Map ] {textAsset.name}");

                    int gKey = grid.Key;
                    foreach (var tKV in grid.NaviTileDict)
                    {
                        int tKey = tKV.Key;
                        var tile = tKV.Value;
                        long id = MapPathUtil.ComputeID(gKey, tKey);

                        Debug.Log($"[NaviID:{id}] nav:{System.Convert.ToString(tile.NaviMask, 16)}, link:{System.Convert.ToString(tile.LinkMask, 2)}");
                    }
                }
            });

            try
            {
                await handle.Task;
            }
            finally
            {
                if (true == handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}
#endif