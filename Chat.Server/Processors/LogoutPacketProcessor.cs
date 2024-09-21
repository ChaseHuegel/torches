using Chat.Server.IO;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.Services;
using Packets.Auth;
using Packets.Chat;

namespace Chat.Server.Processors;

public class LogoutPacketProcessor(
    ChatMessenger chat,
    SmartFormatter formatter,
    ILoginService loginService,
    IPacketProtocol protocol,
    ILogger logger
) : PacketProcessor<LogoutPacket>(chat, formatter, loginService, protocol, logger)
{
    protected override Result<EventBehavior> ProcessPacket(MessageEventArgs<LogoutPacket> e)
    {
        _logger.LogInformation("Logout requested by {Sender}.", e.Sender);

        var loggedOut = _loginService.Logout(e.Sender);
        if (!loggedOut)
        {
            _logger.LogInformation("Logout from {Sender} rejected.\n{Message}", e.Sender, loggedOut.Message);
            return new Result<EventBehavior>(true, EventBehavior.Continue);
        }

        _logger.LogInformation("Logout from {Sender} accepted.", e.Sender);

        Result notifyOthers = _chat.Broadcast(ChatChannel.System, _formatter.Format("{:L:Notifications.UserLoggedOut}", e.Sender), new Except<Session>(e.Sender));
        if (!notifyOthers)
        {
            _logger.LogError("Failed to notify other users of a logout.\n{Message}", notifyOthers.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }
}