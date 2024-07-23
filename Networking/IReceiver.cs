namespace Networking;

public interface IReceiver<T>
{
    event EventHandler<ReceiveEventArgs<T>>? Recv;
}