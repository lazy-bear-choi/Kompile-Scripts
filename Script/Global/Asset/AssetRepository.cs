namespace Script.GamePlay.Global.Asset
{
    using System.Collections.Concurrent;
    using System.Threading; // Interlocked 사용을 위해 추가
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public class AssetRepository
    {
        private class HandleEntry
        {
            public AsyncOperationHandle Handle { get; }
            private int _referenceCount; // 원자적 연산을 위해 필드로 분리

            public int ReferenceCount => _referenceCount;

            public HandleEntry(AsyncOperationHandle handle)
            {
                Handle = handle;
                _referenceCount = 1;
            }

            // [수정 완료] 원자적 증감 연산 적용
            public void AddRef() => Interlocked.Increment(ref _referenceCount);
            public int RemoveRef() => Interlocked.Decrement(ref _referenceCount);
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
                HandleEntry newEntry = new HandleEntry(handle);

                // [수정 완료] 중복 로드 방어: TryAdd 실패 시 생성된 핸들 즉시 해제 후 기존 캐시 반환
                if (_handles.TryAdd(address, newEntry))
                {
                    return handle.Result;
                }
                else
                {
                    Addressables.Release(handle);
                    if (_handles.TryGetValue(address, out HandleEntry existingEntry))
                    {
                        existingEntry.AddRef();
                        return existingEntry.Handle.Result as T;
                    }
                }
            }

            Debug.LogError($"[AssetRepository] 에셋 로드 실패: {address}");
            Addressables.Release(handle);
            return null;
        }

        // [수정 완료] ReleaseAssset -> ReleaseAsset 오타 수정
        public void ReleaseAsset(string address)
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