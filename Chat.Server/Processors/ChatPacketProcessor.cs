using Chat.Server.IO;
using Chat.Server.Types;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking;
using Networking.Events;
using Networking.Services;
using Packets.Chat;

namespace Chat.Server.Processors;

public class ChatPacketProcessor(
    SmartFormatter formatter,
    SessionService sessionService,
    ILoginService loginService,
    IPacketProtocol protocol,
    ILogger logger
) : PacketProcessor<ChatPacket>(formatter, loginService, protocol, logger)
{
    private readonly SessionService _sessionService = sessionService;

    protected override Result<EventBehavior> ProcessPacket(MessageEventArgs<ChatPacket> e)
    {
        ChatPacket chat = e.Message;
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
        var messageToSender = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToTarget = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = _protocol.Send(messageToSender, sender);
        Result sendToTarget = _protocol.Send(messageToTarget, target);
        if (!sendToSender || !sendToTarget)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToTarget.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }

    private Result<ChatPacket> SendLocalBroadcast(ChatPacket chat, Session sender)
    {
        var message = new ChatMessage((int)chat.Channel, sender, null, chat.Value);
        var messageToSender = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToOthers = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = _protocol.Send(messageToSender, sender);
        //  TODO identify local targets
        Result sendToOthers = _protocol.Send(messageToOthers, new Except<Session>(sender));
        if (!sendToSender || !sendToOthers)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToOthers.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }

    private Result<ChatPacket> SendGlobalBroadcast(ChatPacket chat, Session sender)
    {
        var message = new ChatMessage((int)chat.Channel, sender, null, chat.Value);
        var messageToSender = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToOthers = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = _protocol.Send(messageToSender, sender);
        Result sendToOthers = _protocol.Send(messageToOthers, new Except<Session>(sender));
        if (!sendToSender || !sendToOthers)
        {
            return new Result<ChatPacket>(false, chat, StringUtils.JoinValid('\n', sendToSender.Message, sendToOthers.Message));
        }

        return new Result<ChatPacket>(true, chat);
    }
}