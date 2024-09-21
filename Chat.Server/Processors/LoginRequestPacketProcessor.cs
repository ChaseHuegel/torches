using Chat.Server.IO;
using Chat.Server.Types;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.Services;
using Packets.Auth;
using Packets.Chat;

namespace Chat.Server.Processors;

public class LoginRequestPacketProcessor(
    ChatMessenger chat,
    SmartFormatter formatter,
    ILoginService loginService,
    IPacketProtocol protocol,
    ILogger logger
) : PacketProcessor<LoginRequestPacket>(chat, formatter, loginService, protocol, logger)
{
    protected override bool AuthRequired => false;

    protected override Result<EventBehavior> ProcessPacket(MessageEventArgs<LoginRequestPacket> e)
    {
        LoginRequestPacket loginRequest = e.Message;
        _logger.LogInformation("Login requested by {sender}.", e.Sender);

        LoginResponsePacket loginResponse;
        if (_loginService.IsLoggedIn(e.Sender) || _loginService.IsLoggedIn(loginRequest.Token))
        {
            _logger.LogInformation("Login from {sender} rejected: Already logged in.", e.Sender);
            loginResponse = new LoginResponsePacket(false, _formatter.Format("{:L:Auth.Login.AlreadyLoggedIn}"));
        }
        else if (!_loginService.ValidateToken(loginRequest.Token))
        {
            _logger.LogInformation("Login from {sender} rejected: Token invalid.", e.Sender);
            loginResponse = new LoginResponsePacket(false, _formatter.Format("{:L:Auth.Login.InvalidToken}"));
        }
        else if (!_loginService.Login(e.Sender, loginRequest.Token))
        {
            _logger.LogInformation("Login from {sender} rejected: Login failed.", e.Sender);
            loginResponse = new LoginResponsePacket(false, _formatter.Format("{:L:Auth.Login.Failed}"));
        }
        else
        {
            loginResponse = new LoginResponsePacket(true, _formatter.Format("{:L:Auth.Login.Success}"));
        }

        Result sendResult = _protocol.Send(loginResponse, e.Sender);
        if (!sendResult)
        {
            _logger.LogError("Failed to send a login response to {Sender}.\n{Message}", e.Sender, sendResult.Message);
            return new Result<EventBehavior>(false, EventBehavior.Continue, sendResult.Message);
        }

        _logger.LogInformation("Login from {sender} accepted.", e.Sender);

        Result notifyOthers = _chat.Broadcast(ChatChannel.System, _formatter.Format("{:L:Notifications.UserLoggedIn}", e.Sender), new Except<Session>(e.Sender));
        if (!notifyOthers)
        {
            _logger.LogError("Failed to notify other users of a login.\n{Message}", sendResult.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }
}