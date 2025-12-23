namespace Script.Interface
{
    using Script.Index;

    public interface IContentUpdater
    {
        public void OnUpdate();
    }
    public interface IIngameFixedUpdaterV2
    {
        public void OnFixedUpdate();
    }
    public interface IIngameLateUpdaterV2
    {
        public void OnLateUpdae();
    }





    public interface IIngameUpdater
    {
        public IngameUpdateState UpdateState();
    }
    public interface IIngameFixedUpdater
    {
        public IngameUpdateState FixedUpdateState();
    }
    public interface IIngameLateUpdater
    {
        public IngameUpdateState LateUpdateState();
    }
}
