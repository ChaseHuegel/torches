using Packets;
using Packets.Auth;

namespace Networking.Serialization;

public class LoginResponsePacketSerializer : IPacketSerializer<LoginResponsePacket>
{
    public PacketType PacketType => PacketType.LoginResponse;

    public LoginResponsePacket Deserialize(byte[] data)
    {
        return LoginResponsePacket.Deserialize(data, 0, data.Length);
    }

    public byte[] Serialize(LoginResponsePacket value)
    {
        return value.Serialize();
    }
}