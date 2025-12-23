namespace Script.IngameMessage
{
    using UnityEngine;
    using Script.Data;
    using Script.Index;

    public readonly struct OnGetAsset_GameObject
    {
        public readonly AssetCode AssetCode;
        public readonly GameObject GameObject;
        public int InstanceID => GameObject.GetInstanceID();
        public OnGetAsset_GameObject(AssetCode index, GameObject targetObj)
        {
            AssetCode = index;
            GameObject = targetObj;
        }
    }
    public readonly struct OnGetAsset_MapGridData
    {
        public readonly AssetCode AssetCode;
        public readonly MapGridData Data;
        public OnGetAsset_MapGridData(AssetCode index, MapGridData data)
        {
            AssetCode = index;
            Data = data;
        }
    }
    public readonly struct OnSelect_UITitleMenu
    {
        public readonly int ValueInt;
        public OnSelect_UITitleMenu(int value)
        {
            ValueInt = value;
        }
    }

    public readonly struct OnEndEvent
    {
        public readonly IngameEventType EventType;
        public OnEndEvent(IngameEventType ingameEventType)
        {
            EventType = ingameEventType;
        }
    }
}