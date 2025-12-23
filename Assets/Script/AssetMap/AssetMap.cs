namespace AddressasbleAsset
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Addressable Asset Map", menuName = "Tools/Asset/Create Addressable Asset Table")]
    public class AssetMap<TEnum> : ScriptableObject where TEnum : Enum
    {
        // 저장에서만 쓰임
        [SerializeField]
        private List<AssetMapEntry<TEnum>> entry = new List<AssetMapEntry<TEnum>>();

        // 런타임에서만 쓰임 - 메모리를 좀 더 쓰지만, 탐색이 O(1)으로 압도적으로 좋다.
        private Dictionary<TEnum, string> runtimeMap;

        public bool TryGetAddrKey(TEnum id, out string key)
        {
            // 이런 식으로 초기화 하는거 안 좋아하셈;
            if (null == runtimeMap)
            {
                runtimeMap = new Dictionary<TEnum, string>();
                for (int i = 0; i < entry.Count; ++i)
                {
                    runtimeMap.Add(entry[i].ID, entry[i].AddressKey);
                }
            }

            return runtimeMap.TryGetValue(id, out key);
        }
    }
}
