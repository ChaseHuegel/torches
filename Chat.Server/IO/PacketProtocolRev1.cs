using Library.Collections;
using Library.Types;
using Library.Util;
using Networking.LowLevel;
using Networking.Services;
using Packets.Auth;
using Packets.Chat;
using Packets.Entities;
using Packets.World;

namespace Chat.Server.IO;

public class PacketProtocolRev1(ILoginService loginService, IDataSender sender) : IPacketProtocol
{
    protected readonly ILoginService _loginService = loginService;
    protected readonly IDataSender _sender = sender;

    public int ProtocolRevision => 1;

    private bool CanSendToSession(Session session)
    {
        return _loginService.IsLoggedIn(session);
    }

    private Result InternalSend(Packet packet, Session target)
    {
        return _sender.Send(packet.Serialize(), target);
    }

    private Result InternalSend(Packet packet, IFilter<Session> filter)
    {
        var canSendFilter = new Where<Session>(CanSendToSession);
        var sendFilter = new MultiFilter<Session>(canSendFilter, filter);
        return _sender.Send(packet.Serialize(), sendFilter);
    }

    public Result Send(ChatPacket message, Session target)
    {
        var packet = new Packet(PacketType.Chat, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(ChatPacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Chat, 1, message.Serialize());
        return InternalSend(packet, filter);
    }

    public Result Send(TextPacket message, Session target)
    {
        var packet = new Packet(PacketType.Text, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(TextPacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Text, 1, message.Serialize());
        return InternalSend(packet, filter);
    }

    public Result Send(EntityPacket message, Session target)
    {
        var packet = new Packet(PacketType.Entity, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(EntityPacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Entity, 1, message.Serialize());
        return InternalSend(packet, filter);
    }

    public Result Send(LoginRequestPacket message, Session target)
    {
        var packet = new Packet(PacketType.LoginRequest, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(LoginRequestPacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.LoginRequest, 1, message.Serialize());
        return InternalSend(packet, filter);
    }

    public Result Send(LoginResponsePacket message, Session target)
    {
        var packet = new Packet(PacketType.LoginResponse, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(LoginResponsePacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.LoginResponse, 1, message.Serialize());
        return InternalSend(packet, filter);
    }

    public Result Send(LogoutPacket message, Session target)
    {
        var packet = new Packet(PacketType.Logout, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(LogoutPacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Logout, 1, message.Serialize());
        return InternalSend(packet, filter);
    }

    public Result Send(JoinRequestPacket message, Session target)
    {
        var packet = new Packet(PacketType.JoinRequest, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(JoinRequestPacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.JoinRequest, 1, message.Serialize());
        return InternalSend(packet, filter);
    }

    public Result Send(JoinResponsePacket message, Session target)
    {
        var packet = new Packet(PacketType.JoinResponse, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(JoinResponsePacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.JoinResponse, 1, message.Serialize());
        return InternalSend(packet, filter);
    }

    public Result Send(CharacterPacket message, Session target)
    {
        var packet = new Packet(PacketType.Character, 1, message.Serialize());
        return InternalSend(packet, target);
    }

    public Result Send(CharacterPacket message, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Character, 1, message.Serialize());
        return InternalSend(packet, filter);
    }
}