namespace Script.Map.Editor
{
    using System.Collections.Generic;
    using Unity.Collections;
    using UnityEditor;
    using UnityEngine;
    using Script.Map.Runtime;

    /// <summary>
    /// [Editor Only] 에디터 환경에서 맵 타일 데이터를 NativeHashMap으로 캐싱하고 보관하는 창고
    /// </summary>
    [InitializeOnLoad]
    public static class EditorMapCacheRepository
    {
        private static NativeHashMap<long, (long, long)> _nativeMap;
        private static int _lastCount = -1;

        static EditorMapCacheRepository()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
        }

        public static NativeHashMap<long, (long, long)> GetOrCreateNativeMap(Dictionary<long, MapTileData> tileDic, bool forceToCreate = false)
        {
            if (true == _nativeMap.IsCreated && _lastCount == tileDic.Count)
            {
                if (false == forceToCreate)
                {
                    return _nativeMap;
                }
            }

            Clear();

            _nativeMap = new NativeHashMap<long, (long, long)>(tileDic.Count, Allocator.Persistent);
            foreach (var kv in tileDic)
            {
                _nativeMap.TryAdd(kv.Key, (kv.Value.NaviMask, kv.Value.LinkMask));
            }

            Debug.Log($"[MapSampling] 맵 데이터 캐싱 완료: {tileDic.Count}개 타일 (Allocator.Persistent)");

            _lastCount = tileDic.Count;
            return _nativeMap;
        }

        public static void Clear()
        {
            if (true == _nativeMap.IsCreated)
            {
                _nativeMap.Dispose();
                _lastCount = -1;
            }
        }
    }
}