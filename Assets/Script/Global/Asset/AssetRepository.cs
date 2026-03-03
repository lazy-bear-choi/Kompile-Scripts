namespace Script.GamePlay.Global.Asset
{
    using System.Collections.Concurrent;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// 어드레서블 메모리 핸들 및 전역 참조 카운트를 관리
    /// </summary>
    public class AssetRepository
    {
        private class HandleEntry
        { 
            public AsyncOperationHandle Handle { get; }
            public int ReferenceCount { get; private set; }

            public HandleEntry(AsyncOperationHandle handle)
            {
                Handle = handle;
                ReferenceCount = 1;
            }

            public void AddRef() => ++ReferenceCount;
            public int RemoveRef() => --ReferenceCount;
        }

        private readonly ConcurrentDictionary<string, HandleEntry> _handles = new ConcurrentDictionary<string, HandleEntry>();

        public async Awaitable<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (_handles.TryGetValue(address, out HandleEntry entry))
            {
                entry.AddRef();
                return entry.Handle.Result as T;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _handles.TryAdd(address, new HandleEntry(handle));
                return handle.Result;
            }

            Debug.LogError($"[AssetRepository] 에셋 로드 실패: {address}");
            Addressables.Release(handle);
            return null;
        }

        public void ReleaseAssset(string address)
        {
            if (true == _handles.TryGetValue(address, out HandleEntry entry))
            {
                if (0 >= entry.RemoveRef())
                {
                    Addressables.Release(entry.Handle);
                    _handles.TryRemove(address, out var _);
                }
            }
        }
    }
}