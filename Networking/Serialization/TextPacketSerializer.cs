using Library.Serialization;
using Packets;
using Packets.Chat;

namespace Networking.Serialization;

public class TextPacketSerializer : IPacketSerializer<TextPacket>
{
    public PacketType PacketType => PacketType.Text;

    public TextPacket Deserialize(byte[] data)
    {
        return TextPacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(TextPacket value)
    {
        return value.Serialize();
    }
}