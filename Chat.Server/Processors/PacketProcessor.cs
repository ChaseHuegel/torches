using Chat.Server.Types;
using Library.Collections;
using Library.Events;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.LowLevel;
using Networking.Services;
using Packets.Chat;

namespace Chat.Server.Processors;

public abstract class PacketProcessor<T>(
    SmartFormatter formatter,
    ILoginService loginService,
    IDataSender sender,
    ILogger logger
) : IEventProcessor<MessageEventArgs<T>>
{
    protected readonly SmartFormatter _formatter = formatter;
    protected readonly ILoginService _loginService = loginService;
    protected readonly IDataSender _sender = sender;
    protected readonly ILogger _logger = logger;

    protected virtual bool AuthRequired => true;

    protected abstract Result<EventBehavior> ProcessPacket(MessageEventArgs<T> e);

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<T> e)
    {
        if (AuthRequired && !_loginService.IsLoggedIn(e.Sender))
        {
            var message = new ChatMessage((int)ChatChannel.System, new Session(), e.Sender, _formatter.Format("{:L:Auth.Login.LoginRequired}"));
            string text = _formatter.Format("{:L:Chat.Format.Self}", message);
            Result sendLoginRequired = SendTextMessage(new TextPacket(ChatChannel.System, text), e.Sender);
            if (!sendLoginRequired)
            {
                _logger.LogError("Failed to send a message to {Sender}.\n{Message}", e.Sender, sendLoginRequired.Message);
                return new Result<EventBehavior>(false, EventBehavior.Continue, sendLoginRequired.Message);
            }

            return new Result<EventBehavior>(false, EventBehavior.Continue, "Session is not logged into an account.");
        }

        return ProcessPacket(e);
    }

    protected Result SendTextMessage(TextPacket text, Session target)
    {
        var packet = new Packet(PacketType.Text, 1, text.Serialize());
        return _sender.Send(packet.Serialize(), target);
    }

    protected Result SendTextMessage(TextPacket text, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Text, 1, text.Serialize());
        return _sender.Send(packet.Serialize(), new Where<Session>(session => filter.Allowed(session) && _loginService.IsLoggedIn(session)));
    }
}