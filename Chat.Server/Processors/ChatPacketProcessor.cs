using Chat.Server.Types;
using Library.Collections;
using Library.Events;
using Library.Types;
using Library.Util;
using Networking;
using Networking.Events;
using Networking.LowLevel;
using Packets.Chat;

namespace Chat.Server.Processors;

public class ChatPacketProcessor : IEventProcessor<MessageEventArgs<ChatPacket>>
{
    private readonly SmartFormatter _formatter;
    private readonly SessionService _sessionService;
    private readonly IDataSender _sender;
    private readonly ILogger _logger;

    public ChatPacketProcessor(SmartFormatter formatter, SessionService sessionService, IDataSender sender, ILogger logger)
    {
        _formatter = formatter;
        _sessionService = sessionService;
        _sender = sender;
        _logger = logger;
    }

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<ChatPacket> e)
    {
        ChatPacket chat = e.Message;
        _logger.LogInformation("Recv chat on channel: {Channel}, from: {Sender}, to: {DestinationID}, value: \"{Value}\"", chat.Channel, e.Sender, chat.DestinationID, chat.Value);

        Result<ChatPacket> sendResult = SendTextMessage(chat, e.Sender);
        if (!sendResult)
        {
            _logger.LogError("Failed to send a chat on channel: {Channel}, from: {Sender}.\n{Message}", chat.Channel, e.Sender, sendResult.Message);
            return new Result<EventBehavior>(false, EventBehavior.Continue, sendResult.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }

    private Result<ChatPacket> SendTextMessage(ChatPacket chat, Session sender)
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
        return _sender.Send(packet.Serialize(), filter);
    }
}