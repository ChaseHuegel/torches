using Library.ECS;
using Library.ECS.Components;
using Library.Events;
using Library.Util;
using Networking.Events;
using Packets.Entities;

namespace World.Client.Processors;

public class EntityPacketProcessor(ILogger logger, ECSContext ecs) : IEventProcessor<MessageEventArgs<EntityPacket>>
{
    private readonly ILogger _logger = logger;
    private readonly ECSContext _ecs = ecs;

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<EntityPacket> e)
    {
        EntityPacket entity = e.Message;

        _ecs.DataStore.AddOrUpdate(entity.ID, new IdentifierComponent(entity.Name));
        _ecs.DataStore.AddOrUpdate(entity.ID, new PositionComponent(entity.Position.X, entity.Position.Y, entity.Position.Z));

        _ecs.Tick();

        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }
}