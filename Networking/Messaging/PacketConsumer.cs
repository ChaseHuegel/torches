using Library.Serialization;
using Networking.Events;
using Networking.LowLevel;
using Packets;

namespace Networking.Messaging;

public class PacketConsumer<T> : MessageConsumer<T>, IDisposable
{
    public PacketConsumer(IPacketSerializer<T> serializer, IDataProducer[] dataProducers)
        : base(serializer, dataProducers) { }

    protected override void OnDataReceived(object? sender, DataEventArgs e)
    {
        Packet packet = Packet.Deserialize(e.Data, 0, e.Data.Length);
        if (packet.Type != ((IPacketSerializer<T>)_serializer).PacketType)
        {
            return;
        }

        base.OnDataReceived(sender, new DataEventArgs(packet.Data, e.Sender));
    }
}