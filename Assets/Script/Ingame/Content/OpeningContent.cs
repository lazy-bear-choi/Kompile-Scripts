namespace Script.Content
{
    using Script.Manager;
    using System;
    using System.Threading;
    using UnityEngine;

    public class OpeningContent : ContentBase
    {
        public override async Awaitable EnterAync()
        {
            // load assets
            GameObject titleObject = await AssetManagerV2.GetOrNewInstanceAsync(PrefabID.OP_TITLE_OBJECT, IngameManager.UIOverayRootTransform);
            var title = titleObject.GetComponent<TitleObject>();
            child_instance.Add(title);

            try
            {
                await title.PlayLogoSequence();
            }
            catch (OperationCanceledException)
            {
                await title.ExitLogoSequence();
            }

            await title.PlayTitleSequence();

            // active: title menu
            UITitleMenuObject titleMenu = title.SetActiveTitleMenu();
            child_updater.Add(titleMenu);
        }
        public override void Exit()
        {
            IngameUpdateManager.Unregister(this);
            // 여기서 1프레임 기다렸다가 다음에 삭제하는 절차가 더 안전할까?

            for (int i = child_instance.Count; i >= 0; --i)
            {
                AssetManagerV2.ReleaseInstance(child_instance[i]);
            }

            child_updater = null;
        }
    }
}