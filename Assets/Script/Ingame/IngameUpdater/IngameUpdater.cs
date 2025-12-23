namespace Script.Manager
{
    using Script.Index;
    using Script.Interface;
    using System.Collections.Generic;
    using UnityEngine;

    public class IngameUpdater : MonoBehaviour
    {
        private static List<IContentUpdater> updateList;
        private static List<IIngameFixedUpdater> fixedUpdateList;
        private static List<IIngameLateUpdater> lateUpdateList;

        public void Initialize()
        {
            updateList = new List<IContentUpdater>();
            fixedUpdateList = new List<IIngameFixedUpdater>();
            lateUpdateList = new List<IIngameLateUpdater>();
        }

        public static void AddUpdater(IContentUpdater updater)
        {
            updateList.Add(updater);
        }
        public static void AddFixedUpdater(IIngameFixedUpdater fixedUpdater)
        {
            fixedUpdateList.Add(fixedUpdater);
        }
        public static void AddLateUpdater(IIngameLateUpdater lateUpdater)
        {
            lateUpdateList.Add(lateUpdater);
        }

        public static void RemoveUpdater(IContentUpdater updater)
        {
            updateList.Remove(updater);
        }
        public static void RemoveFixedUpdater(IIngameFixedUpdater fixedUpdater)
        {
            fixedUpdateList.Remove(fixedUpdater);
        }
        public static void RemoveLateUpdater(IIngameLateUpdater lateUpdater)
        {
            lateUpdateList.Remove(lateUpdater);
        }

        private void Update()
        {
            for (int i = 0; i < updateList.Count; ++i)
            {
                // update 다 돌린 후에 해당 updater.enabled = false;를 하면 알아서 updaterList에서 날린다.
#if UNITY_EDITOR
                //Debug.Assert(IngameUpdateState.FAILURE != updateList[i].UpdateState());
#else
                //updateList[i].UpdateState();
#endif
            }
        }
        private void FixedUpdate()
        {
            for (int i = 0; i < fixedUpdateList.Count; ++i)
            {
#if UNITY_EDITOR
                Debug.Assert(IngameUpdateState.FAILURE != fixedUpdateList[i].FixedUpdateState());
#else
                fixedUpdateList[i].FixedUpdateState();
#endif
            }
        }
        private void LateUpdate()
        {
            for (int i = 0; i < lateUpdateList.Count; ++i)
            {
#if UNITY_EDITOR
                Debug.Assert(IngameUpdateState.FAILURE != lateUpdateList[i].LateUpdateState());
#else
                lateUpdateList[i].LateUpdateState();
#endif
            }
        }
    }
}