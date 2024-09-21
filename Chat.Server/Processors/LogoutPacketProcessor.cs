using Chat.Server.Types;
using Library.Collections;
using Library.Events;
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
) : IEventProcessor<MessageEventArgs<LogoutPacket>>
{
    private readonly SmartFormatter _formatter = formatter;
    private readonly ILoginService _loginService = loginService;
    private readonly IDataSender _sender = sender;
    private readonly ILogger _logger = logger;

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<LogoutPacket> e)
    {
        ChatMessage message;

        if (!_loginService.IsLoggedIn(e.Sender))
        {
            message = new ChatMessage((int)ChatChannel.System, new Session(), e.Sender, _formatter.Format("{:L:Auth.Login.LoginRequired}"));
            string text = _formatter.Format("{:L:Chat.Format.Self}", message);
            Result sendLoginRequired = SendTextMessage(new TextPacket(ChatChannel.System, text), e.Sender);
            if (!sendLoginRequired)
            {
                _logger.LogError("Failed to send a message to {Sender}.\n{Message}", e.Sender, sendLoginRequired.Message);
                return new Result<EventBehavior>(false, EventBehavior.Continue, sendLoginRequired.Message);
            }

            return new Result<EventBehavior>(false, EventBehavior.Continue, "User is not logged in.");
        }

        _logger.LogInformation("Logout requested by {Sender}.", e.Sender);

        var loggedOut = _loginService.Logout(e.Sender);
        if (!loggedOut)
        {
            _logger.LogInformation("Logout from {Sender} rejected.\n{Message}", e.Sender, loggedOut.Message);
            return new Result<EventBehavior>(true, EventBehavior.Continue);
        }

        _logger.LogInformation("Logout from {Sender} accepted.", e.Sender);

        message = new ChatMessage((int)ChatChannel.System, default, e.Sender, _formatter.Format("{:L:Notifications.UserLoggedOut}", e.Sender));
        var messageToOthers = new TextPacket(ChatChannel.System, _formatter.Format("{:L:Chat.Format.Other}", message));
        Result sendLoggedOut = SendTextMessage(messageToOthers, new Except<Session>(e.Sender));
        if (!sendLoggedOut)
        {
            _logger.LogError("Failed to notify other users of a logout.\n{Message}", sendLoggedOut.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }

    private Result SendTextMessage(TextPacket text, Session target)
    {
        var packet = new Packet(PacketType.Text, 1, text.Serialize());
        return _sender.Send(packet.Serialize(), target);
    }

    private Result SendTextMessage(TextPacket text, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Text, 1, text.Serialize());
        return _sender.Send(packet.Serialize(), new Where<Session>(session => filter.Allowed(session) && _loginService.IsLoggedIn(session)));
    }
}