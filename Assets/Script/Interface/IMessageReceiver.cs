using Script.Index;
using System.Threading.Tasks;

namespace Script.Interface
{
    public interface IMessageReceiver
    {
        public Task<bool> ReceiveIngameMessage<T>(T data) where T : struct;
    }

    public interface IInputReceiver
    {
        public bool ReceiveInput(IDxInput.InputFlag flag);
    }
}