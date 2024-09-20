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

public class LoginRequestPacketProcessor(
    SmartFormatter formatter,
    ILoginService loginService,
    IDataSender sender,
    ILogger logger
) : IEventProcessor<MessageEventArgs<LoginRequestPacket>>
{
    private readonly SmartFormatter _formatter = formatter;
    private readonly ILoginService _loginService = loginService;
    private readonly IDataSender _sender = sender;
    private readonly ILogger _logger = logger;

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<LoginRequestPacket> e)
    {
        LoginRequestPacket loginRequest = e.Message;
        _logger.LogInformation("Login requested by {sender}.", e.Sender);

        LoginResponsePacket loginResponse;
        if (_loginService.IsLoggedIn(e.Sender) || _loginService.IsLoggedIn(loginRequest.Token))
        {
            _logger.LogInformation("Login from {sender} rejected: Already logged in.", e.Sender);
            loginResponse = new LoginResponsePacket(1, false, _formatter.Format("{:L:Auth.Login.AlreadyLoggedIn}"));
        }
        else if (!_loginService.ValidateToken(loginRequest.Token))
        {
            _logger.LogInformation("Login from {sender} rejected: Token invalid.", e.Sender);
            loginResponse = new LoginResponsePacket(1, false, _formatter.Format("{:L:Auth.Login.InvalidToken}"));
        }
        else if (!_loginService.Login(e.Sender, loginRequest.Token))
        {
            _logger.LogInformation("Login from {sender} rejected: Login failed.", e.Sender);
            loginResponse = new LoginResponsePacket(1, false, _formatter.Format("{:L:Auth.Login.Failed}"));
        }
        else
        {
            loginResponse = new LoginResponsePacket(1, true, _formatter.Format("{:L:Auth.Login.Success}"));
        }

        Result sendResult = SendLoginResponse(loginResponse, e.Sender);
        if (!sendResult)
        {
            _logger.LogError("Failed to send a login response to {Sender}.\n{Message}", e.Sender, sendResult.Message);
            return new Result<EventBehavior>(false, EventBehavior.Continue, sendResult.Message);
        }

        _logger.LogInformation("Login from {sender} accepted.", e.Sender);

        var message = new ChatMessage((int)ChatChannel.System, default, e.Sender, _formatter.Format("{:L:Notifications.UserLoggedIn}", e.Sender));
        var messageToOthers = new TextPacket(1, ChatChannel.System, _formatter.Format("{:L:Chat.Format.Other}", message));
        sendResult = SendTextMessage(messageToOthers, new Except<Session>(e.Sender));
        if (!sendResult)
        {
            _logger.LogError("Failed to notify other users of a login.\n{Message}", sendResult.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }

    private Result SendLoginResponse(LoginResponsePacket loginResponse, Session target)
    {
        var packet = new Packet(PacketType.LoginResponse, loginResponse.Serialize());
        return _sender.Send(packet.Serialize(), target);
    }

    private Result SendTextMessage(TextPacket text, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Text, text.Serialize());
        return _sender.Send(packet.Serialize(), new Where<Session>(session => filter.Allowed(session) && _loginService.IsLoggedIn(session)));
    }
}