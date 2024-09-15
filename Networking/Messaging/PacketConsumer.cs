using Networking.Events;
using Networking.LowLevel;
using Networking.Serialization;
using Packets;

namespace Networking.Messaging;

public class PacketConsumer<T>(
    IPacketSerializer<T> serializer,
    IDataProducer[] dataProducers
) : MessageConsumer<T>(serializer, dataProducers)
{
    public PacketAwaiter<T> GetPacketAwaiter()
    {
        return new PacketAwaiter<T>(this);
    }

    protected override void OnDataReceived(object? sender, DataEventArgs e)
    {
        Packet packet = Packet.Deserialize(e.Data, 0, e.Data.Length);
        IPacketSerializer<T> serializer = (IPacketSerializer<T>)_serializer;
        if (packet.Type != serializer.PacketType)
        {
            return;
        }

        base.OnDataReceived(sender, new DataEventArgs(packet.Data, e.Sender));
    }
}