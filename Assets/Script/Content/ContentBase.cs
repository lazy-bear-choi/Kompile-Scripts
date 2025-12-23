namespace Script.Content
{
    using Script.Interface;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;

    public abstract class ContentBase : IContentUpdater
    {
        protected List<IngameMonoBehaviourBase> child_instance = new List<IngameMonoBehaviourBase>();
        protected List<IContentUpdater>         child_updater  = new List<IContentUpdater>();

        public abstract Awaitable EnterAync();
        public abstract void Exit();
        public void OnUpdate()
        {
            for (int i = child_updater.Count - 1; i >= 0; --i)
            {
                child_updater[i].OnUpdate();
            }
        }

        ~ContentBase()
        {
            for (int i = child_updater.Count; i >= 0; --i)
            {

            }
        }
    }
}