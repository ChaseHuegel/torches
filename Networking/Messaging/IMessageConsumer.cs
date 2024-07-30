using Networking.Events;

namespace Networking.Messaging;

public interface IMessageConsumer<T>
{
    event EventHandler<MessageEventArgs<T>>? NewMessage;
}
