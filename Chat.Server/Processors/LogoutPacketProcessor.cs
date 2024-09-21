using Chat.Server.Types;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.LowLevel;
using Networking.Services;
using Packets.Auth;
using Packets.Chat;

namespace Chat.Server.Processors;

public class LogoutPacketProcessor(
    SmartFormatter formatter,
    ILoginService loginService,
    IDataSender sender,
    ILogger logger
) : PacketProcessor<LogoutPacket>(formatter, loginService, sender, logger)
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

        var message = new ChatMessage((int)ChatChannel.System, default, e.Sender, _formatter.Format("{:L:Notifications.UserLoggedOut}", e.Sender));
        var messageToOthers = new TextPacket(ChatChannel.System, _formatter.Format("{:L:Chat.Format.Other}", message));
        Result sendLoggedOut = SendTextMessage(messageToOthers, new Except<Session>(e.Sender));
        if (!sendLoggedOut)
        {
            _logger.LogError("Failed to notify other users of a logout.\n{Message}", sendLoggedOut.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }
}