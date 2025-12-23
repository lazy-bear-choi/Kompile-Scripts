namespace Script.Manager
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Script.Index;
    using MessagePack;
    using Script.Data;
    using System.IO;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;

#endif
    
    /// <summary>
    /// 다시 보니까 AssetManager의 개념 정의도 애매모호한 것 같은데?
    /// 'ingame 외부의 에셋을 관리/제어한다'로 정의하는게 맞지 않을까...
    /// 우선 아닌 것부터 싹 다 날려야겠네.
    /// </summary>
    public static partial class AssetManager
    {
        // Map?
        public static async Task<T> GetAssetAsync<T>(string address) where T : Object
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            T asset = handle.Result;
            _nonGameObjectInstances.TryAdd(asset.GetInstanceID(), handle);

            return asset;
        }
    }
    public static partial class AssetManager
    {
        private static readonly ConcurrentDictionary<string, InstanceEntry>     _gameObjectInstances    = new ConcurrentDictionary<string, InstanceEntry>(); // 풀링까지 고려
        private static readonly ConcurrentDictionary<int, AsyncOperationHandle> _nonGameObjectInstances = new ConcurrentDictionary<int, AsyncOperationHandle>();

        private static readonly System.Threading.SynchronizationContext mainSyncContext = System.Threading.SynchronizationContext.Current;

        // Table Data (Binary)
        public static async Task<T> ReadBinaryFileAsync<T>(string key)
        {
            // 어드레서블 에셋 로드
            AsyncOperationHandle<IList<TextAsset>> handle = Addressables.LoadAssetsAsync<TextAsset>(key, null);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result.Count == 0)
            {
                throw new FileNotFoundException($"라벨에 해당하는 파일이 존재하지 않습니다: {key}");
            }

            // 파일에서 바이트 배열 읽기 및 역직렬화
            byte[] serializedData = handle.Result[0].bytes;
            T data = MessagePackSerializer.Deserialize<T>(serializedData);

            // T라는 데이터로 저장했으니 원본 TextAsset은 가지고 있을 이유가 없다. -> 곧장 해제
            handle.ReleaseHandleOnCompletion();
            return data;
        }

        // GameObject Instance
        public static async Task<GameObject> GetOrNewInstanceAsync(AssetCode assetCode, Transform parent, bool usePooling = false)
        {
            string key = assetCode.ToString();

            if (false == _gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var obj = handle.Result;
                    var instance_id = obj.GetInstanceID();
                    _nonGameObjectInstances.TryAdd(instance_id, handle);
                    Debug.Log($"[AssetManager] Successfully loaded asset: {key}");
                }
                else
                {
                    Debug.LogError($"[AssetManager] Failed to load asset '{key}'. Status: {handle.Status}, Exception: {handle.OperationException}");
                    throw new System.Exception($"Failed to load asset: {key}. Error: {handle.OperationException}");
                }

                entry = new InstanceEntry(handle, usePooling);
                _gameObjectInstances.TryAdd(key, entry);
            }

            GameObject instance;

            if (true == entry.HasPooledInstance())
            {
                instance = entry.Pool.Dequeue();
                instance.SetActive(true);
            }
            else
            {
                AsyncOperationHandle<GameObject> instHandle = Addressables.InstantiateAsync(key, parent);
                instance = await instHandle.Task;
            }

            entry.AddReference();
            return instance;
        }
        public static async Task<T> GetOrNewInstanceAsync<T>(AssetCode assetCode, Transform parent, bool usePooling = false) where T:MonoBehaviour
        {
            string key = assetCode.ToString();

            if (false == _gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var obj = handle.Result;
                    var hash_code = obj.GetHashCode();
                    _nonGameObjectInstances.TryAdd(hash_code, handle);
                    Debug.Log($"[AssetManager] Successfully loaded asset: {key}");
                }
                else
                {
                    Debug.LogError($"[AssetManager] Failed to load asset '{key}'. Status: {handle.Status}, Exception: {handle.OperationException}");
                    throw new System.Exception($"Failed to load asset: {key}. Error: {handle.OperationException}");
                }

                entry = new InstanceEntry(handle, usePooling);
                _gameObjectInstances.TryAdd(key, entry);
            }

            T instance;

            if (true == entry.HasPooledInstance())
            {
                instance = entry.Pool.Dequeue() as T;
                instance.gameObject.SetActive(true);
            }
            else
            {
                AsyncOperationHandle<GameObject> instHandle = Addressables.InstantiateAsync(key, parent);
                instance = (await instHandle.Task).GetComponent<T>();
            }

            entry.AddReference();
            return instance;
        }
        public static async Task<(int HashCode, T Value)> LoadAssetAsync<T>(string key) where T : class
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            T value = await handle.Task;

            var hash_code = value.GetHashCode();
            _nonGameObjectInstances.TryAdd(hash_code, handle);

            return (hash_code, value);
        }


        // 얘네를 하나로 통합하는게 제일 베스트일 것 같습니다만...
        public static void ReleaseInstance(AssetCode assetCode, GameObject instance, bool forced = false)
        {
            string key = assetCode.ToString();

#if UNITY_EDITOR
            int id = instance.GetInstanceID();
#endif

            if (false == _gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                return;
            }

            if (true == entry.UsePooling
                && false == forced)
            {
                instance.SetActive(false);
                entry.Pool.Enqueue(instance);
            }
            else
            {
                Addressables.ReleaseInstance(instance);
#if UNITY_EDITOR
                Debug.Log($"[AssetManager] Release Instance [{assetCode}] (id:{id})");
#endif
            }

            entry.RemoveReference();

            if (true == entry.ShouldRelease())
            {
                Addressables.Release(entry.Handle);
                _gameObjectInstances.TryRemove(key);

#if UNITY_EDITOR
                Debug.Log($"[AssetManager] Release Asset Handler [{assetCode}]");
#endif
            }
        }
        public static void ReleaseInstance(int instanceID)
        {
            mainSyncContext.Post((state) =>
            {
                if (_nonGameObjectInstances.TryGetValue(instanceID, out var handle))
                {
#if UNITY_EDITOR
                    Debug.Log($"[AssetManager] Release[{handle.Result}] (id: {instanceID})");
#endif

                    Addressables.Release(handle);
                    _nonGameObjectInstances.TryRemove(instanceID);
                }
            },
            null);
        }
    }


#if UNITY_EDITOR
    public static partial class AssetManager
    {
        public static void WriteBinaryFile<T>(T data, string dataPath, string fileName, string addressableGroup = null)
        {
            // 1. 경로 설정 (Path.Combine 권장)
            // dataPath는 "Data/Maps" 처럼 Assets 하위 경로라고 가정
            string fullDirectoryPath = Path.Combine(Application.dataPath, dataPath);
            string filePath = Path.Combine(fullDirectoryPath, fileName + ".bytes");
            string assetPath = Path.Combine("Assets", dataPath, fileName + ".bytes");

            if (!Directory.Exists(fullDirectoryPath))
            {
                Directory.CreateDirectory(fullDirectoryPath);
            }

            // 2. 직렬화 옵션 체크 (로드할 때와 반드시 동일해야 함)
            // 이전 코드에서 ContractlessStandardResolver를 썼다면 여기서도 동일하게 설정
            var options = MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            try
            {
                byte[] serializedData = MessagePackSerializer.Serialize(data, options);
                File.WriteAllBytes(filePath, serializedData);

                // 파일이 물리적으로 생성된 후 에디터가 인식하도록 갱신
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Serialization Failed: {e.Message}");
                return;
            }

            // 3. 어드레서블 설정
            if (!string.IsNullOrEmpty(addressableGroup))
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null) return;

                AddressableAssetGroup group = settings.FindGroup(addressableGroup)
                                             ?? settings.CreateGroup(addressableGroup, false, false, false, null);

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

                if (entry != null)
                {
                    entry.SetAddress(fileName);
                    
                    // Label 설정이 꼭 필요한 경우가 아니라면 생략 가능하지만, 유지한다면 아래와 같이
                    //entry.SetLabel(fileName, true);

                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[DataExporter] Saved and Addressable registered: {assetPath}");
                }
            }
        }
    }
}
#endif

