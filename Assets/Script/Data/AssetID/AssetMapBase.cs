using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class AssetEntry<TEnum>
{
    public TEnum Id;
    public string AddressKey;

    public AssetEntry() 
    {

    }
}

public class AssetMapBase<TEnum> : ScriptableObject, IInitializable where TEnum : Enum
{
    // 에디터 스크립트가 SerializedObject를 통해 접근하는 필드
    // 필드 이름("entries")은 AssetMapGenerator의 FindProperty()와 일치해야 한다.
    [SerializeField]
    protected List<AssetEntry<TEnum>> entries = new List<AssetEntry<TEnum>>();

    // 런타임에 빠른 조회를 위한 딕셔너리
    private Dictionary<TEnum, string> runtimeMap;

    /// <summary>
    /// 런타임에 한 번만 호출하여 딕셔너리를 초기화
    /// </summary>
    public virtual void Initialize()
    {
        if (null != runtimeMap)
        {
            return;
        }

        runtimeMap = new Dictionary<TEnum, string>();
        for (int i = 0; i < entries.Count; ++i)
        {
            runtimeMap.TryAdd(entries[i].Id, entries[i].AddressKey);
        }
    }

    public string GetAddressKey(TEnum id)
    {
        if (null == runtimeMap)
        {
            Initialize();
        }

        if (true == runtimeMap.TryGetValue(id, out string addressKey))
        {
            return addressKey;
        }

#if UNITY_EDITOR
        Debug.LogError($"Can`t find Addressable Asset: {id}");
#endif
        return null;
    }
}
