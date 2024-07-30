using Library.Types;

namespace Networking.Events;

public readonly struct MessageEventArgs<T>(T message, Session sender)
{
    public readonly T Message = message;

    public readonly Session Sender = sender;
}