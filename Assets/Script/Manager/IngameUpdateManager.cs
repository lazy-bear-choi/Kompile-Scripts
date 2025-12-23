namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Interface;

    public class IngameUpdateManager : MonoBehaviour
    {
        private static IngameUpdateManager instance;

        private readonly List<IContentUpdater> contentUpdates = new List<IContentUpdater>();

        public void Awake()
        {
            if (null != instance)
            {
                Destroy(this.gameObject);
                return;
            }
            instance = this;
        }

        public static void Register(IContentUpdater updater)
        {
            if (false == instance.contentUpdates.Contains(updater))
            {
                instance.contentUpdates.Add(updater);
            }
        }
        public static void Unregister(IContentUpdater updater)
        {
            if (true == instance.contentUpdates.Contains(updater))
            {
                instance.contentUpdates.Remove(updater);
            }
        }

        private void Update()
        {
            // float deltaTime = Time.deltaTime; // 필요 시 사용

            for (int i = contentUpdates.Count - 1; i >= 0; --i)
            {
                if (null != contentUpdates[i])
                {
                    contentUpdates[i].OnUpdate();
                }
                else
                {
                    contentUpdates.RemoveAt(i);
                }
            }
        }
    }
}