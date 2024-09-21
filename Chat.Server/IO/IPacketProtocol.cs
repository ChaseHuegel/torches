using Library.Collections;
using Library.Types;
using Library.Util;
using Packets.Auth;
using Packets.Chat;
using Packets.Entities;

namespace Chat.Server.IO;

public interface IPacketProtocol
{
    int ProtocolRevision { get; }

    Result Send(ChatPacket message, Session target);
    Result Send(ChatPacket message, IFilter<Session> filter);

    Result Send(TextPacket message, Session target);
    Result Send(TextPacket message, IFilter<Session> filter);

    Result Send(EntityPacket message, Session target);
    Result Send(EntityPacket message, IFilter<Session> filter);

    Result Send(LoginRequestPacket message, Session target);
    Result Send(LoginRequestPacket message, IFilter<Session> filter);

    Result Send(LoginResponsePacket message, Session target);
    Result Send(LoginResponsePacket message, IFilter<Session> filter);

    Result Send(LogoutPacket message, Session target);
    Result Send(LogoutPacket message, IFilter<Session> filter);
}