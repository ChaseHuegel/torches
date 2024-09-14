using Packets;
using Packets.Auth;

namespace Networking.Serialization;

public class LoginRequestPacketSerializer : IPacketSerializer<LoginRequestPacket>
{
    public PacketType PacketType => PacketType.LoginRequest;

    public LoginRequestPacket Deserialize(byte[] data)
    {
        return LoginRequestPacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(LoginRequestPacket value)
    {
        return value.Serialize();
    }
}