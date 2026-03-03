namespace Script.Map.Runtime
{
    using MessagePack;
    using MessagePack.Resolvers;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using Script.GamePlay.Global.Data;

    /// <summary>
    /// 1. 맵 데이터 로드<br/>
    /// 2. 길찾기 성능 최적화를 위해 NativeHashMap으로 캐싱(Caching) 및 메모리 수명을 관리
    /// </summary>
    public class MapDataRepository
    {
        private Dictionary<int, List<MapGridLayerData>> _gridLayerDic;
        private Dictionary<long, MapTileData> _tileDic;
        private Dictionary<int3, long> _posToID;

        private NativeHashMap<long, (long NaviMask, long LinkMask)> _nativeTileMap;

        public Dictionary<long, MapTileData> TileDic => _tileDic;

        // NativeHashMap은 내부적으로 포인터를 포함한 구조체. 일반적인 프로퍼티로 반환하면 호출할 때마다 구조체 복사(Copy by Value)가 발생
        // ref readolny struct => 메모리 주소만 슥 건네주어(ref) 내부 비트 플래그를 빠르게 검사하게 하되,
        // 맵 데이터 자체가 오염되지 않도록 **잠금(readonly)**을 걸어둔 것
        public ref readonly NativeHashMap<long, (long NaviMask, long LinkMask)> NativeTileMap => ref _nativeTileMap;

        public MapDataRepository()
        {
            _gridLayerDic = new Dictionary<int, List<MapGridLayerData>>();
            _tileDic = new Dictionary<long, MapTileData>();
            _posToID = new Dictionary<int3, long>();
        }

        public async Awaitable LoadFromAddressableAsync(string gridAddress)
        {
            TextAsset ta = await Repository.Asset.LoadAssetAsync<TextAsset>(gridAddress);
            if (null == ta)
            {
                Debug.LogError($"[MapDataRepository] AssetRepository Load Failed: {gridAddress}");
                return;
            }

            // Key 지정 방식: 인덱스(0, 1, 2...) 대신 변수 이름(String)을 Key로 사용하여 데이터를 저장합니다. (JSON과 유사한 방식)
            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            try
            {
                MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(ta.bytes, options);
                Initialize(grid);
                BuildNativeCache();

                // 사용(파싱)이 끝난 에셋은 전역 AssetRepository에 해제(Release)를 요청
                Repository.Asset.ReleaseAssset(gridAddress);

                Debug.Log($"[MapDataRepository] Load {grid.NaviTileDict.Keys.Count} nodes from '{gridAddress}'");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MapDataRepository] Deserialize Error: {e.Message}");
            }
        }
        private void Initialize(MapGridData grid)
        {
            int count = grid.NaviTileDict.Keys.Count;
            int gKey = grid.Key;

            _gridLayerDic.TryAdd(gKey, grid.layerMeshAssets);

            int tKey;
            MapTileData tile;
            long id;
            foreach (var tileKV in grid.NaviTileDict)
            {
                tKey = tileKV.Key;
                tile = tileKV.Value;

                id = MapPathUtil.ComputeID(gKey, tKey);
                if (false == _tileDic.TryAdd(id, tile))
                {
                    _tileDic[id] = tile;
                }

                int3 absPivot = MapPathUtil.ComputeWorldPositionInt(id);
                _posToID.TryAdd(absPivot, id);
            }
        }

        private void BuildNativeCache()
        {
            if (true == _nativeTileMap.IsCreated)
            {
                _nativeTileMap.Dispose();
            }

            _nativeTileMap = new NativeHashMap<long, (long NaviMask, long LinkMask)>(_tileDic.Count, Allocator.Persistent);
            foreach (var kv in _tileDic)
            {
                _nativeTileMap.TryAdd(kv.Key, (kv.Value.NaviMask, kv.Value.LinkMask));
            }
        }

        public void Clear()
        {
            _gridLayerDic.Clear();
            _tileDic.Clear();
            _posToID.Clear();

            if (true == _nativeTileMap.IsCreated)
            {
                _nativeTileMap.Dispose();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용: 특정 라벨(MapNavi)의 모든 에셋을 일괄 로드하여 테스트합니다.
        /// (전역 AssetRepository는 단일 Address 로드만 지원하므로, 에디터 테스트용에 한하여 Addressables를 직접 사용합니다)
        /// </summary>
        public async Awaitable EditLoadAllAsync()
        {
            string label = "MapNavi";
            var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
            {
                if (null != textAsset)
                {
                    var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                    MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(textAsset.bytes, options);
                    Initialize(grid);
                    Debug.Log($"[TEST][Load Baked Map] {textAsset.name}");
                }
            });
            await handle.Task;
            Addressables.Release(handle);

            BuildNativeCache();
        }
#endif
    }
}