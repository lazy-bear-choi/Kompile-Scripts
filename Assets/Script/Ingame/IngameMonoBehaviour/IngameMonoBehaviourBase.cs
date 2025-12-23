using Script.Interface;
using Script.Manager;
using Script.Index;
using UnityEngine;

public abstract class IngameMonoBehaviourBase : MonoBehaviour
{
    public PrefabID PrefabID;

//    protected AssetCode asset_code;

//    protected virtual void OnEnable()
//    {
//        if (this is IContentUpdater updater)
//        {
//            IngameUpdater.AddUpdater(updater);
//#if UNITY_EDITOR
//            Debug.Log($"[IngameMonoBehaviourBase] Add Updater({updater.GetType().Name})");
//#endif
//        }
//        if (this is IIngameFixedUpdater fixedUpdater)
//        {
//            IngameUpdater.AddFixedUpdater(fixedUpdater);
//        }
//        if (this is IIngameLateUpdater lateUpdater)
//        {
//            IngameUpdater.AddLateUpdater(lateUpdater);
//        }
//        if (this is IInputReceiver inputReceiver)
//        {
//            InputHandler.AddInputReceiver(inputReceiver);
//        }
//    }
//    protected virtual void OnDisable()
//    {
//        // 얘네도 비동기로 여차저차 처리하는 게 가능할 것 같기도 한데...

//        if (this is IContentUpdater updater)
//        {
//            IngameUpdater.RemoveUpdater(updater);
//        }
//        if (this is IIngameFixedUpdater fixedUpdater)
//        {
//            IngameUpdater.RemoveFixedUpdater(fixedUpdater);
//        }
//        if (this is IIngameLateUpdater lateUpdater)
//        {
//            IngameUpdater.RemoveLateUpdater(lateUpdater);
//        }
//        if (this is IInputReceiver inputReceiver)
//        {
//            InputHandler.RemoveInputReceiver(inputReceiver);
//        }
//    }

//    public abstract void Release();
}
