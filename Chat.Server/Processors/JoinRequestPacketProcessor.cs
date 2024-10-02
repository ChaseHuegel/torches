using Chat.Server.IO;
using Chat.Server.Types;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.Services;
using Packets.Auth;
using Packets.Chat;
using Packets.Entities;
using Packets.World;

namespace Chat.Server.Processors;

public class JoinRequestPacketProcessor(
    ChatMessenger chat,
    SmartFormatter formatter,
    ILoginService loginService,
    IPacketProtocol protocol,
    ILogger logger
) : PacketProcessor<JoinRequestPacket>(chat, formatter, loginService, protocol, logger)
{
    protected override bool AuthRequired => true;

    protected override Result<EventBehavior> ProcessPacket(MessageEventArgs<JoinRequestPacket> e)
    {
        JoinRequestPacket joinRequest = e.Message;
        _logger.LogInformation("Join requested by {sender} as {character}.", e.Sender, joinRequest.CharacterUID);

        var joinResponse = new JoinResponsePacket(true, _formatter.Format("{:L:Auth.Join.Success}"));

        //  TODO Validate character isn't already in the world
        //  TODO Validate character exists
        //  TODO Validate character is owned by sender's login

        Result sendResult = _protocol.Send(joinResponse, e.Sender);
        if (!sendResult)
        {
            _logger.LogError("Failed to send a join response to {Sender}.\n{Message}", e.Sender, sendResult.Message);
            return new Result<EventBehavior>(false, EventBehavior.Continue, sendResult.Message);
        }

        _logger.LogInformation("Join as {character} from {sender} accepted.", joinRequest.CharacterUID, e.Sender);

        Result notifyOthers = _chat.Broadcast(ChatChannel.System, _formatter.Format("{:L:Notifications.CharacterJoined}", e.Sender), new Except<Session>(e.Sender));
        if (!notifyOthers)
        {
            _logger.LogError("Failed to notify other users of a join.\n{Message}", sendResult.Message);
        }

        var characterPacket = new CharacterPacket(joinRequest.CharacterUID, "NoName", "", 0, new CharacterCustomizations());
        Result sendCharacter = _protocol.Send(characterPacket, new All<Session>());
        if (!sendCharacter)
        {
            _logger.LogError("Failed to send a newly joined character.\n{Message}", sendResult.Message);
        }

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }
}