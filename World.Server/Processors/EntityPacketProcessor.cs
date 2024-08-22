using Library.Collections;
using Library.Events;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.LowLevel;
using Packets.Chat;

namespace World.Server.Processors;

public class EntityPacketProcessor(ILogger logger, IDataSender sender) : IEventProcessor<MessageEventArgs<EntityPacket>>
{
    private readonly ILogger _logger = logger;
    private readonly IDataSender _sender = sender;

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<EntityPacket> e)
    {
        EntityPacket entity = e.Message;
        _logger.LogInformation("Recv entity {entity} from {sender}.", entity.ID, e.Sender);

        SendEntityPacket(entity, new All<Session>());

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }

    private Result SendEntityPacket(EntityPacket entity, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Entity, entity.Serialize());
        return _sender.Send(packet.Serialize(), filter);
    }
}