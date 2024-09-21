using Library.Collections;
using Library.ECS;
using Library.ECS.Components;
using Library.Types;
using Library.Util;
using Networking.LowLevel;
using Packets;
using Packets.Entities;
using Swordfish.Library.Collections;
using Swordfish.Library.IO;

namespace World.Client.Commands;

public class MoveCommand(IDataSender sender, ECSContext ecs) : Command
{
    public override string Option => "move";
    public override string Description => "Move an entity in a direction.";
    public override string ArgumentsHint => "<entity> <n/e/s/w>";

    private readonly IDataSender _sender = sender;
    private readonly ECSContext _ecs = ecs;

    protected override Task<CommandState> InvokeAsync(ReadOnlyQueue<string> args)
    {
        if (!int.TryParse(args.Take(), out int entity))
        {
            return Task.FromResult(CommandState.Failure);
        }

        var identifierComponent = _ecs.DataStore.Query<IdentifierComponent>(entity);
        var positionQuery = _ecs.DataStore.Query<PositionComponent>(entity);
        var position = new Position(positionQuery.Value.X, positionQuery.Value.Y, positionQuery.Value.Z);
        string direction = args.Take();
        switch (direction)
        {
            case "n":
                position.Y += 1;
                break;
            case "s":
                position.Y -= 1;
                break;
            case "e":
                position.X += 1;
                break;
            case "w":
                position.X -= 1;
                break;
            default:
                return Task.FromResult(CommandState.Failure);
        }

        var entityPacket = new EntityPacket(entity, identifierComponent.Value.Name ?? $"Entity {entity}", position, 0f);
        SendEntityPacket(entityPacket, new All<Session>());
        return Task.FromResult(CommandState.Success);
    }

    private Result SendEntityPacket(EntityPacket entity, IFilter<Session> filter)
    {
        var packet = new Packet(PacketType.Entity, 1, entity.Serialize());
        return _sender.Send(packet.Serialize(), filter);
    }
}