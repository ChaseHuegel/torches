using Chat.Server.IO;
using Chat.Server.Types;
using Library.Events;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.Services;
using Packets.Chat;

namespace Chat.Server.Processors;

public abstract class PacketProcessor<T>(
    ChatMessenger chat,
    SmartFormatter formatter,
    ILoginService loginService,
    IPacketProtocol protocol,
    ILogger logger
) : IEventProcessor<MessageEventArgs<T>>
{
    protected readonly ChatMessenger _chat = chat;
    protected readonly SmartFormatter _formatter = formatter;
    protected readonly ILoginService _loginService = loginService;
    protected readonly IPacketProtocol _protocol = protocol;
    protected readonly ILogger _logger = logger;

    protected virtual bool AuthRequired => true;

    protected abstract Result<EventBehavior> ProcessPacket(MessageEventArgs<T> e);

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<T> e)
    {
        if (AuthRequired && !_loginService.IsLoggedIn(e.Sender))
        {
            Result sendLoginRequired = _chat.Message(ChatChannel.System, _formatter.Format("{:L:Auth.Login.LoginRequired}"), e.Sender);
            if (!sendLoginRequired)
            {
                _logger.LogError("Failed to send a message to {Sender}.\n{Message}", e.Sender, sendLoginRequired.Message);
                return new Result<EventBehavior>(false, EventBehavior.Continue, sendLoginRequired.Message);
            }

            return new Result<EventBehavior>(false, EventBehavior.Continue, "Session is not logged into an account.");
        }

        return ProcessPacket(e);
    }
}