using Library.ECS;
using Library.ECS.Components;

namespace World.Client.Systems;

public class PositionLogSystem(DataStore store) : EntitySystem<IdentifierComponent, PositionComponent>(store)
{
    protected override void OnTick(int entity, ref IdentifierComponent identifier, ref PositionComponent position)
    {
        Console.WriteLine($"{identifier.Name} ({entity}) is at: {position.X},{position.Y},{position.Z}");
    }
}