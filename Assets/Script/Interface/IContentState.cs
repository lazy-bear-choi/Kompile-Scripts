using Script.Interface;
using UnityEngine;

public interface IContentState : IContentUpdater
{
    public Awaitable EnterAync();
    public void Exit();
}
