namespace Script.Manager
{
    using MessagePack;
    using Script.Data;
    using Script.Index;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using static UnityEngine.EventSystems.EventTrigger;

    /// <summary>
    /// 역할: (1)자원 로딩 (2)서브 시스템 초기화 (3)시스템간 통신 중재
    /// </summary>
    public static class AssetManagerV2
    {
        private static Dictionary<Type, ScriptableObject> assetReferenceCache;

        private static ConcurrentDictionary<string, InstanceEntry>      gameObjectInstances;
        private static ConcurrentDictionary<int, AsyncOperationHandle>  nonGameObjectInstances;
        private static SynchronizationContext                           mainSyncContext;

        public static void Initialize()
        {
            assetReferenceCache = new Dictionary<Type, ScriptableObject>();
            LoadAllAssetMaps();

            gameObjectInstances     = new ConcurrentDictionary<string, InstanceEntry>();
            nonGameObjectInstances  = new ConcurrentDictionary<int, AsyncOperationHandle>();
            mainSyncContext         = SynchronizationContext.Current;
        }


        // Binary File
        // ...


        // Addressable Asset Map
        private static void LoadAllAssetMaps()
        {
            ScriptableObject[] maps = Resources.LoadAll<ScriptableObject>("AssetMap");
            if (maps.Length == 0)
            {

                return;
            }

            ScriptableObject map;
            Type mapType, baseType, enumType;
            for (int i = 0; i < maps.Length; ++i)
            {
                map = maps[i];

                mapType = map.GetType();
                baseType = mapType.BaseType; //상속한 부모 타입 찾는거구나?

                // 조건문 무슨 말인지 잘 모르겠네
                while (baseType != null
                    && baseType.IsGenericType
                    && baseType.GetGenericTypeDefinition() != typeof(AssetMapBase<>))
                {
                    baseType = baseType.BaseType;
                }

                if (baseType != null
                    && baseType.IsGenericType)
                {
                    enumType = baseType.GetGenericArguments()[0];

                    if (true == assetReferenceCache.ContainsKey(enumType))
                    {
                        // 중복 무시
                        continue;
                    }

                    assetReferenceCache.Add(enumType, map);
                    if (map is IInitializable initializeMap)
                    {
                        initializeMap.Initialize();
                    }
                }
            }
        }
        public static string GetAssetAddress<TEnum>(TEnum id) where TEnum : Enum
        {
            Type enumType = typeof(TEnum);

            if (true == assetReferenceCache.TryGetValue(enumType, out ScriptableObject map))
            {
                var assetMap = map as AssetMapBase<TEnum>;
                if (assetMap != null)
                {
                    return assetMap.GetAddressKey(id);
                }
            }

            return null;
        }

        public static async Awaitable<GameObject> GetOrNewInstanceAsync<TEnum>(TEnum id, Transform parent, bool usePooling = true) where TEnum : Enum
        {
            string assetAddress = GetAssetAddress(id);
            if (false == gameObjectInstances.TryGetValue(assetAddress, out InstanceEntry entry)) 
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(assetAddress);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var obj = handle.Result;
                    var instance_id = obj.GetInstanceID();
                    nonGameObjectInstances.TryAdd(instance_id, handle);
                    Debug.Log($"[AssetManager] Successfully loaded asset: {id}");
                }
                else
                {
                    Debug.LogError($"[AssetManager] Failed to load asset '{id}'. Status: {handle.Status}, Exception: {handle.OperationException}");
                    throw new System.Exception($"Failed to load asset: {id}. Error: {handle.OperationException}");
                }

                entry = new InstanceEntry(handle, usePooling);
                gameObjectInstances.TryAdd(assetAddress, entry);
            }

            // 오브젝트 풀에서 꺼내어 사용
            GameObject instance;
            if (true == entry.HasPooledInstance())
            {
                instance = entry.Pool.Dequeue();
                instance.SetActive(true);
            }
            else
            {
                AsyncOperationHandle<GameObject> instHandle = Addressables.InstantiateAsync(assetAddress, parent);
                instance = await instHandle.Task;
            }

            entry.AddReference();
            return instance;
        }
        public static void ReleaseInstance<TEnum>(TEnum id, GameObject instance, bool forced = false) where TEnum: Enum
        {
            string assetAddress = GetAssetAddress(id);
            if (false == gameObjectInstances.TryGetValue(assetAddress, out InstanceEntry entry))
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
                Debug.Log($"[AssetManager] Release Instance ({id})");
#endif
            }

            entry.RemoveReference();

            if (true == entry.ShouldRelease())
            {
                Addressables.Release(entry.Handle);
                gameObjectInstances.TryRemove(assetAddress);

#if UNITY_EDITOR
                Debug.Log($"[AssetManager] Release Asset Handler ({id})");
#endif
            }
        }
        public static void ReleaseInstance(IngameMonoBehaviourBase instance, bool forced = false)
        {
            PrefabID prefabID = instance.PrefabID;
            GameObject obj = instance.gameObject;
#if UNITY_EDITOR
            int instanceID = obj.GetInstanceID();
#endif

            string assetAddress = GetAssetAddress(prefabID);
            if (false == gameObjectInstances.TryGetValue(assetAddress, out InstanceEntry entry))
            {
                return;
            }

            if (true == entry.UsePooling
                && false == forced)
            {
                obj.SetActive(false);
                entry.Pool.Enqueue(obj);
            }
            else
            {
                Addressables.ReleaseInstance(obj);

#if UNITY_EDITOR
                Debug.Log($"[AssetManager] Release Instance ({prefabID}, {instanceID})");
#endif
            }

            entry.RemoveReference();

            if (true == entry.ShouldRelease())
            {
                Addressables.Release(entry.Handle);
                gameObjectInstances.TryRemove(assetAddress);

#if UNITY_EDITOR
                Debug.Log($"[AssetManager] Release Asset Handler ({prefabID})");
#endif
            }
        }
    }
}
