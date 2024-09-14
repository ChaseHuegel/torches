using System.Runtime.CompilerServices;
using Networking.Events;
using Networking.LowLevel;
using Networking.Serialization;
using Packets;

namespace Networking.Messaging;

public class PacketConsumer<T> : MessageConsumer<T>
{
    public PacketConsumer(IPacketSerializer<T> serializer, IDataProducer[] dataProducers)
        : base(serializer, dataProducers) { }

    public PacketAwaiter<T> GetPacketAwaiter()
    {
        return new PacketAwaiter<T>(this);
    }

    public TaskAwaiter<T> GetAwaiter()
    {
        return new PacketAwaiter<T>(this).GetAwaiter();
    }

    protected override void OnDataReceived(object? sender, DataEventArgs e)
    {
        Packet packet = Packet.Deserialize(e.Data, 0, e.Data.Length);
        IPacketSerializer<T> serializer = (IPacketSerializer<T>)_serializer;
        if (packet.Type != serializer.PacketType)
        {
            return;
        }

        Console.WriteLine($"got packet: {packet.Type}");
        base.OnDataReceived(sender, new DataEventArgs(packet.Data, e.Sender));
    }
}