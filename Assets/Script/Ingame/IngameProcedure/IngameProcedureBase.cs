namespace Script.Content
{
    using Script.Index;
    using Script.Interface;
    using Script.Manager;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class IngameProcedureBase
    {
        protected IngameProcedureType procedureType;
        protected List<(AssetCode code, GameObject obj)> ingameObjects;

        public abstract Task<bool> Start();

        public bool IsType(IngameProcedureType type)
        {
            return type == procedureType;
        }

        protected abstract Task<bool> ExecuteIngameEventAsync(IngameEventType eventType);

        /// <summary> 신경 안쓰고 싶어서 virtual로 일괄 Dispose <br/>
        /// 필요하면 override 하여 기능 추가 </summary>
        public virtual void Dispose()
        {
            if (this is IMessageReceiver receiver)
            {
                MessageManager.Dispose(receiver);
            }

            for (int i = 0; i < ingameObjects.Count; ++i)
            {
                AssetManager.ReleaseInstance(ingameObjects[i].code, ingameObjects[i].obj);
            }
        }

        protected async Task<T> GetIngameObjectAsync<T>(AssetCode assetCode, Transform parent) where T:IngameMonoBehaviourBase
        {
            for (int i = 0; i < ingameObjects.Count; ++i)
            {
                if (assetCode == ingameObjects[i].code)
                {
                    return ingameObjects[i].obj.GetComponent<T>();
                }
            }

            GameObject obj = await AssetManager.GetOrNewInstanceAsync(assetCode, parent);
            if (null == obj)
            {
                return null;
            }

            return obj.GetComponent<T>();
        }

        public IngameProcedureBase()
        {
            if (this is IMessageReceiver receiver)
            {
                MessageManager.AddReceiver(receiver);
            }

            ingameObjects = new List<(AssetCode, GameObject)>();
        }
    }
}