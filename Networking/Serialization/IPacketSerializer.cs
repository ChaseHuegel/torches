using Packets;

namespace Library.Serialization;

public interface IPacketSerializer<T> : ISerializer<T>
{
    PacketType PacketType { get; }
}