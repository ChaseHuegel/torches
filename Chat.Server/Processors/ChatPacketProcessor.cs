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
    SessionService sessionService,
    ChatMessenger chat,
    SmartFormatter formatter,
    ILoginService loginService,
    IPacketProtocol protocol,
    ILogger logger
) : PacketProcessor<ChatPacket>(chat, formatter, loginService, protocol, logger)
{
    private readonly SessionService _sessionService = sessionService;

    protected override Result<EventBehavior> ProcessPacket(MessageEventArgs<ChatPacket> e)
    {
        ChatPacket chat = e.Message;
        _logger.LogInformation("Recv chat on channel: {Channel}, from: {Sender}, to: {DestinationID}, value: \"{Value}\"", chat.Channel, e.Sender, chat.DestinationID, chat.Value);

        Result relay = RelayChatAsText(chat, e.Sender);
        if (!relay)
        {
            _logger.LogError("Failed to relay chat on channel: {Channel}, from: {Sender}.\n{Message}", chat.Channel, e.Sender, relay.Message);
            return new Result<EventBehavior>(false, EventBehavior.Continue, relay.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }

    private Result RelayChatAsText(ChatPacket chat, Session sender)
    {
        switch (chat.Channel)
        {
            case ChatChannel.Whisper:
                return RelayWhisper(chat, sender);
            case ChatChannel.Local:
                //  TODO filter to nearby targets
                return RelayOnChannel(chat.Channel, sender, chat.Value);
            case ChatChannel.System:
            case ChatChannel.Global:
            case ChatChannel.Help:
            case ChatChannel.Trade:
                return RelayOnChannel(chat.Channel, sender, chat.Value);
            default:
                return new Result(false, $"Unsupported {nameof(ChatChannel)}: {chat.Channel}.");
        }
    }

    private Result RelayWhisper(ChatPacket chat, Session sender)
    {
        if (chat.DestinationID == null)
        {
            return new Result(false, $"No target {nameof(ChatPacket.DestinationID)} provided.");
        }

        Result<Session> target = _sessionService.Get(chat.DestinationID.Value);
        if (!target)
        {
            return new Result(false, $"Session does not exist for {nameof(ChatPacket.DestinationID)}: {chat.DestinationID}.");
        }

        var message = new ChatMessage((int)chat.Channel, sender, target, chat.Value);
        var messageToSender = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Self}", message));
        var messageToTarget = new TextPacket(chat.Channel, _formatter.Format("{:L:Chat.Format.Other}", message));

        Result sendToSender = _protocol.Send(messageToSender, sender);
        Result sendToTarget = _protocol.Send(messageToTarget, target);
        if (!sendToSender || !sendToTarget)
        {
            return new Result(false, StringUtils.JoinValid('\n', sendToSender.Message, sendToTarget.Message));
        }

        return new Result(true);
    }

    private Result RelayOnChannel(ChatChannel channel, Session sender, string message)
    {
        var chat = new ChatMessage((int)channel, sender, null, message);
        var textToSender = new TextPacket(channel, _formatter.Format("{:L:Chat.Format.Self}", chat));
        var textToOthers = new TextPacket(channel, _formatter.Format("{:L:Chat.Format.Other}", chat));

        Result sendToSender = _protocol.Send(textToSender, sender);
        Result sendToOthers = _protocol.Send(textToOthers, new Except<Session>(sender));
        if (!sendToSender || !sendToOthers)
        {
            return new Result(false, StringUtils.JoinValid('\n', sendToSender.Message, sendToOthers.Message));
        }

        return new Result(true);
    }
}