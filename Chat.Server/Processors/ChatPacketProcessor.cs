using Chat.Server.Types;
using Library.Collections;
using Library.Events;
using Library.Types;
using Library.Util;
using Networking;
using Networking.Events;
using Networking.LowLevel;
using Networking.Services;
using Packets.Chat;

namespace Chat.Server.Processors;

public class ChatPacketProcessor(
    SmartFormatter formatter,
    SessionService sessionService,
    ILoginService loginService,
    IDataSender sender,
    ILogger logger
) : IEventProcessor<MessageEventArgs<ChatPacket>>
{
    private readonly SmartFormatter _formatter = formatter;
    private readonly SessionService _sessionService = sessionService;
    private readonly ILoginService _loginService = loginService;
    private readonly IDataSender _sender = sender;
    private readonly ILogger _logger = logger;

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<ChatPacket> e)
    {
        ChatPacket chat = e.Message;

        if (!_loginService.IsLoggedIn(e.Sender))
        {
            var message = new ChatMessage((int)ChatChannel.System, new Session(), e.Sender, _formatter.Format("{:L:Auth.Login.LoginRequired}"));
            var text = _formatter.Format("{:L:Chat.Format.Self}", message);
            Result sendResult = SendTextMessage(new TextPacket(1, ChatChannel.System, text), e.Sender);
            if (!sendResult)
            {
                _logger.LogError("Failed to send a message to {Sender}.\n{Message}", e.Sender, sendResult.Message);
                return new Result<EventBehavior>(false, EventBehavior.Continue, sendResult.Message);
            }

            return new Result<EventBehavior>(false, EventBehavior.Continue, "User is not logged in.");
        }

        _logger.LogInformation("Recv chat on channel: {Channel}, from: {Sender}, to: {DestinationID}, value: \"{Value}\"", chat.Channel, e.Sender, chat.DestinationID, chat.Value);

        Result<ChatPacket> relayResult = RelayChatAsText(chat, e.Sender);
        if (!relayResult)
        {
            _logger.LogError("Failed to relay chat on channel: {Channel}, from: {Sender}.\n{Message}", chat.Channel, e.Sender, relayResult.Message);
            return new Result<EventBehavior>(false, EventBehavior.Continue, relayResult.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }

    private Result<ChatPacket> RelayChatAsText(ChatPacket chat, Session sender)
    {
        switch (chat.Channel)
        {
            case ChatChannel.Whisper:
                return SendWhisper(chat, sender);
            case ChatChannel.Local:
                return SendLocalBroadcast(chat, sender);
            case ChatChannel.System:
            case ChatChannel.Global:
            case ChatChannel.Help:
            case ChatChannel.Trade:
                return SendGlobalBroadcast(chat, sender);
            default:
                return new Result<ChatPacket>(false, chat, $"Unsupported {nameof(ChatChannel)}: {chat.Channel}.");
        }
    }

    private Result<ChatPacket> SendWhisper(ChatPacket chat, Session sender)
    {
        if (chat.DestinationID == null)
        {
            return new Result<ChatPacket>(false, chat, $"No target {nameof(ChatPacket.DestinationID)} provided.");
        }

        Result<Session> target = _sessionService.Get(chat.DestinationID.Value);
        if (!target)
        {
            return new Result<ChatPacket>(false, chat, $"Session does not exist for {nameof(ChatPacket.DestinationID)}: {chat.DestinationID}.");
        }

        var message = new ChatMessage((int)chat.Channel, sender, target, chat.Value);
        var messageToSender = new TextPacket(1, chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToTarget = new TextPacket(1, chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = SendTextMessage(messageToSender, sender);
        Result sendToTarget = SendTextMessage(messageToTarget, target);
        if (!sendToSender || !sendToTarget)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToTarget.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }

    private Result<ChatPacket> SendLocalBroadcast(ChatPacket chat, Session sender)
    {
        var message = new ChatMessage((int)chat.Channel, sender, null, chat.Value);
        var messageToSender = new TextPacket(1, chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToOthers = new TextPacket(1, chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = SendTextMessage(messageToSender, sender);
        //  TODO identify local targets
        Result sendToOthers = SendTextMessage(messageToOthers, new Except<Session>(sender));
        if (!sendToSender || !sendToOthers)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToOthers.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }

    private Result<ChatPacket> SendGlobalBroadcast(ChatPacket chat, Session sender)
    {
        var message = new ChatMessage((int)chat.Channel, sender, null, chat.Value);
        var messageToSender = new TextPacket(1, chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToOthers = new TextPacket(1, chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = SendTextMessage(messageToSender, sender);
        Result sendToOthers = SendTextMessage(messageToOthers, new Except<Session>(sender));
        if (!sendToSender || !sendToOthers)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToOthers.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }

    private Result SendTextMessage(TextPacket text, Session target)
    {
        var packet = new Packet(PacketType.Text, text.Serialize());
        return _sender.Send(packet.Serialize(), target);
    }

    private Result SendTextMessage(TextPacket text, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Text, text.Serialize());
        return _sender.Send(packet.Serialize(), new Where<Session>(session => filter.Allowed(session) && _loginService.IsLoggedIn(session)));
    }
}