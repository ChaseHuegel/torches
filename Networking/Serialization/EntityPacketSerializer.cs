using Packets;
using Packets.Entities;

namespace Networking.Serialization;

public class EntityPacketSerializer : IPacketSerializer<EntityPacket>
{
    public PacketType PacketType => PacketType.Entity;

    public EntityPacket Deserialize(byte[] data)
    {
        return EntityPacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(EntityPacket value)
    {
        return value.Serialize();
    }
}