using Networking.Events;

namespace Networking.LowLevel;

public interface IDataReceiver
{
    event EventHandler<DataEventArgs>? Received;
}