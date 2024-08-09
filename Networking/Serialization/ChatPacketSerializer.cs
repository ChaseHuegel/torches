using Packets;
using Packets.Chat;

namespace Networking.Serialization;

public class ChatPacketSerializer : IPacketSerializer<ChatPacket>
{
    public PacketType PacketType => PacketType.Chat;

    public ChatPacket Deserialize(byte[] data)
    {
        return ChatPacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(ChatPacket value)
    {
        return value.Serialize();
    }
}