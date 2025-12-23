using Script.Interface;
using Script.Manager;

namespace Script.Content
{
    public class ContentStateMachine
    {
        private ContentBase currentState;
        private bool isTransitioning = false;

        public async void ChangeState(ContentBase newContent)
        {
            if (true == isTransitioning)
            {
                return;
            }
            isTransitioning = true;

            // exit before state
            if (null != currentState)
            {
                currentState.Exit();
            }

            // enter new state async
            currentState = newContent;
            if (null != currentState)
            {
                await currentState.EnterAync();
            }

            if (newContent is IContentUpdater newUpdater)
            {
                IngameUpdateManager.Register(newUpdater);
            }

            isTransitioning = false;
        }

        ~ContentStateMachine()
        {
            currentState.Exit();
        }
    }
}
