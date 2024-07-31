using Chat.Server.Types;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking;
using Networking.Events;
using Networking.Messaging;
using Packets.Chat;

namespace Chat.Server;

public class ChatServer
{
    private readonly SmartFormatter _formatter;
    private readonly SessionService _sessionService;
    private readonly IMessageConsumer<ChatPacket> _chatConsumer;
    private readonly IMessageProducer<TextPacket> _textProducer;
    private readonly ILogger _logger;

    public ChatServer(SmartFormatter formatter, SessionService sessionService, IMessageConsumer<ChatPacket> chatConsumer, IMessageProducer<TextPacket> textProducer, ILogger logger)
    {
        _formatter = formatter;
        _sessionService = sessionService;
        _chatConsumer = chatConsumer;
        _textProducer = textProducer;
        _logger = logger;

        _chatConsumer.NewMessage += OnNewChatPacket;
    }

    public void Start()
    {
    }

    private void OnNewChatPacket(object? sender, MessageEventArgs<ChatPacket> e)
    {
        ChatPacket chat = e.Message;
        _logger.LogInformation("Recv chat on channel: {Channel}, from: {Sender}, to: {DestinationID}, value: \"{Value}\"", chat.Channel, e.Sender, chat.DestinationID, chat.Value);

        Result<ChatPacket> sendResult = SendChat(chat, e.Sender);
        if (!sendResult)
        {
            _logger.LogError("Failed to send a chat on channel: {Channel}, from: {Sender}.\n{Message}", chat.Channel, e.Sender, sendResult.Message);
        }
    }

    private Result<ChatPacket> SendChat(ChatPacket chat, Session sender)
    {
        switch (chat.Channel)
        {
            case ChatChannel.Whisper:
                return ProcessWhisper(chat, sender);
            case ChatChannel.Local:
                return ProcessLocalBroadcast(chat, sender);
            case ChatChannel.System:
            case ChatChannel.Global:
            case ChatChannel.Help:
            case ChatChannel.Trade:
                return ProcessGlobalBroadcast(chat, sender);
            default:
                return new Result<ChatPacket>(false, chat, $"Unsupported {nameof(ChatChannel)}: {chat.Channel}.");
        }
    }

    private Result<ChatPacket> ProcessWhisper(ChatPacket chat, Session sender)
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
        var messageToSender = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToTarget = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = _textProducer.Send(messageToSender, sender);
        Result sendToTarget = _textProducer.Send(messageToTarget, target);
        if (!sendToSender || !sendToTarget)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToTarget.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }

    private Result<ChatPacket> ProcessLocalBroadcast(ChatPacket chat, Session sender)
    {
        var message = new ChatMessage((int)chat.Channel, sender, null, chat.Value);
        var messageToSender = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToOthers = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = _textProducer.Send(messageToSender, sender);
        //  TODO identify local targets
        Result sendToOthers = _textProducer.Send(messageToOthers, new Except<Session>(sender));
        if (!sendToSender || !sendToOthers)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToOthers.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }

    private Result<ChatPacket> ProcessGlobalBroadcast(ChatPacket chat, Session sender)
    {
        var message = new ChatMessage((int)chat.Channel, sender, null, chat.Value);
        var messageToSender = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToOthers = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = _textProducer.Send(messageToSender, sender);
        Result sendToOthers = _textProducer.Send(messageToOthers, new Except<Session>(sender));
        if (!sendToSender || !sendToOthers)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToOthers.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }
}