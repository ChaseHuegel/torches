using Packets;
using Packets.Entities;

namespace Networking.Serialization;

public class CharacterPacketSerializer : IPacketSerializer<CharacterPacket>
{
    public PacketType PacketType => PacketType.Character;

    public CharacterPacket Deserialize(byte[] data)
    {
        return CharacterPacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(CharacterPacket value)
    {
        return value.Serialize();
    }
}