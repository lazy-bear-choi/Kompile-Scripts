namespace AddressasbleAsset
{
    using System;

    [Serializable]
    public struct AssetMapEntry<TEnum> where TEnum : Enum
    {
        public TEnum ID;
        public string AddressKey;
    }
}
