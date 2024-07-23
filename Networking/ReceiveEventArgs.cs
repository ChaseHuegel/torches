using Library.Types;

namespace Networking;

public readonly struct ReceiveEventArgs<T>(T message, Session sender)
{
    public readonly T Message = message;
    public readonly Session Sender = sender;
}
