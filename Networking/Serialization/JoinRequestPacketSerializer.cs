using Packets;
using Packets.World;

namespace Networking.Serialization;

public class JoinRequestPacketSerializer : IPacketSerializer<JoinRequestPacket>
{
    public PacketType PacketType => PacketType.JoinRequest;

    public JoinRequestPacket Deserialize(byte[] data)
    {
        return JoinRequestPacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(JoinRequestPacket value)
    {
        return value.Serialize();
    }
}