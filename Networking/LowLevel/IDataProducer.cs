using Networking.Events;

namespace Networking.LowLevel;

public interface IDataProducer
{
    event EventHandler<DataEventArgs>? Received;
}
