using Library.Collections;
using Library.ECS;
using Library.ECS.Components;
using Library.Events;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.LowLevel;
using Packets.Entities;

namespace World.Server.Processors;

public class EntityPacketProcessor(ILogger logger, IDataSender sender, ECSContext ecs) : IEventProcessor<MessageEventArgs<EntityPacket>>
{
    private readonly ILogger _logger = logger;
    private readonly IDataSender _sender = sender;
    private readonly ECSContext _ecs = ecs;

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<EntityPacket> e)
    {
        EntityPacket entity = e.Message;
        _logger.LogInformation("Recv entity {entity} from {sender}.", entity.ID, e.Sender);

        SendEntityPacket(entity, new All<Session>());

        _ecs.DataStore.AddOrUpdate(entity.ID, new PositionComponent(entity.Position.X, entity.Position.Y, entity.Position.Z));

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }

    private Result SendEntityPacket(EntityPacket entity, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Entity, entity.Serialize());
        return _sender.Send(packet.Serialize(), filter);
    }
}