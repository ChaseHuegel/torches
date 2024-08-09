using Library.Serialization;
using Packets;

namespace Networking.Serialization;

public interface IPacketSerializer<T> : ISerializer<T>
{
    PacketType PacketType { get; }
}