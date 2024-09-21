using Chat.Server.Types;
using Library.Collections;
using Library.Types;
using Library.Util;
using Packets.Chat;

namespace Chat.Server.IO;

public class ChatMessenger(SmartFormatter formatter, IPacketProtocol protocol)
{
    private SmartFormatter _formatter = formatter;
    private IPacketProtocol _protocol = protocol;

    public Result Broadcast(ChatChannel channel, string message)
    {
        var chat = new ChatMessage((int)channel, default, default, message);
        var text = new TextPacket(channel, _formatter.Format("{:L:Chat.Format.Other}", chat));
        return _protocol.Send(text, new All<Session>());
    }

    public Result Broadcast(ChatChannel channel, string message, IFilter<Session> filter)
    {
        var chat = new ChatMessage((int)channel, default, default, message);
        var text = new TextPacket(channel, _formatter.Format("{:L:Chat.Format.Other}", chat));
        return _protocol.Send(text, filter);
    }

    public Result Message(ChatChannel channel, string message, Session target)
    {
        var chat = new ChatMessage((int)channel, default, default, message);
        var text = new TextPacket(channel, _formatter.Format("{:L:Chat.Format.Other}", chat));
        return _protocol.Send(text, target);
    }
}