using Packets;
using Packets.Auth;

namespace Networking.Serialization;

public class LogoutPacketSerializer : IPacketSerializer<LogoutPacket>
{
    public PacketType PacketType => PacketType.Logout;

    public LogoutPacket Deserialize(byte[] data)
    {
        return LogoutPacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(LogoutPacket value)
    {
        return value.Serialize();
    }
}