using Packets;
using Packets.World;

namespace Networking.Serialization;

public class JoinResponsePacketSerializer : IPacketSerializer<JoinResponsePacket>
{
    public PacketType PacketType => PacketType.JoinResponse;

    public JoinResponsePacket Deserialize(byte[] data)
    {
        return JoinResponsePacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(JoinResponsePacket value)
    {
        return value.Serialize();
    }
}