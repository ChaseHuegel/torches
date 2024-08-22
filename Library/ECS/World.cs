using Swordfish.Library.Extensions;

namespace Library.ECS;

public class SimpleWorld(int size)
{
    private int _lastEntity = 0;

    public SimpleArchetype<NamedEntity?> NamedEntities = new(size);
    public SimpleArchetype<LocationEntity?> LocationEntities = new(size);
    public SimpleArchetype<LivingEntity?> LivingEntities = new(size);

    public int[] Query(params Type[] types)
    {
        HashSet<int> entities = [];

        if (types.Length == 1 && types[0] == typeof(IdentityComponent))
        {
            foreach (int entity in NamedEntities.Query())
            {
                entities.Add(entity);
            }
        }

        if (types.Length == 1 && types[0] == typeof(PositionComponent))
        {
            foreach (int entity in LocationEntities.Query())
            {
                entities.Add(entity);
            }
        }

        if (types.Length == 2 && types.Contains(typeof(IdentityComponent)) && types.Contains(typeof(PositionComponent)))
        {
            foreach (int entity in LivingEntities.Query())
            {
                entities.Add(entity);
            }
        }

        return [.. entities];
    }

    public int Create(params object[] components)
    {
        int entity = NewEntity();

        if (components.Length == 1 && components[0] is IdentityComponent identity)
        {
            NamedEntities.Entities[entity] = new NamedEntity()
            {
                Identity = identity
            };
        }

        if (components.Length == 1 && components[0] is PositionComponent position)
        {
            LocationEntities.Entities[entity] = new LocationEntity()
            {
                Position = position
            };
        }

        if (components.Length == 2 && components[0] is IdentityComponent identity2 && components[1] is PositionComponent position2)
        {
            LivingEntities.Entities[entity] = new LivingEntity()
            {
                Identity = identity2,
                Position = position2
            };
        }

        return entity;
    }

    private int NewEntity()
    {
        return Interlocked.Increment(ref _lastEntity);
    }
}

public class SimpleArchetype<T>(int size)
{
    public T[] Entities = new T[size];

    public IEnumerable<int> Query()
    {
        for (int i = 0; i < Entities.Length; i++)
        {
            if (Entities[i] == null)
            {
                continue;
            }

            yield return i;
        }
    }
}

public struct NamedEntity
{
    public required IdentityComponent Identity;
}

public struct LocationEntity
{
    public required PositionComponent Position;
}

public struct LivingEntity
{
    public required IdentityComponent Identity;
    public required PositionComponent Position;
}

public struct IdentityComponent
{
    public string Name;
    public string Tag;
}

public struct PositionComponent
{
    public float X;
    public float Y;
    public float Z;
}

public class Archetype(HashSet<Type> componentTypes, int size)
{
    public readonly HashSet<Type> ComponentTypes = componentTypes;
    public readonly int Size = size;
    public readonly Dictionary<Type, object?[]> Components = [];
}

public class ArchetypeSet(Type componentType)
{
    public readonly Type ComponentType = componentType;
    public readonly List<Archetype> Archetypes = [];
}

public class EntityStore
{
    public Dictionary<Type, ArchetypeSet> ArchetypeMap = [];
}

public class World(int size)
{
    private int _lastEntity = 0;
    private readonly int _size = size;
    private readonly EntityStore _entityStore = new();

    public int[] Query(params Type[] types)
    {
        HashSet<int> entities = [];
        var queryTypes = new HashSet<Type>(types);

        foreach (Type type in types)
        {
            if (!_entityStore.ArchetypeMap.TryGetValue(type, out ArchetypeSet? set))
            {
                continue;
            }

            var archetype = set.Archetypes.FirstOrDefault(x => queryTypes.SetEquals(x.ComponentTypes));
            if (archetype == null)
            {
                continue;
            }

            if (!archetype.Components.TryGetValue(type, out object?[]? archetypeComponents))
            {
                continue;
            }

            for (int i = 0; i < archetypeComponents.Length; i++)
            {
                if (archetypeComponents[i] == null)
                {
                    continue;
                }

                entities.Add(i);
            }
        }

        return [.. entities];
    }

    public int Create(params object[] components)
    {
        int entity = NewEntity();
        HashSet<Type> componentTypes = components.Select(x => x.GetType()).ToHashSet();

        foreach (object component in components)
        {
            Type type = component.GetType();

            ArchetypeSet set = _entityStore.ArchetypeMap.GetOrAdd(type, ArchetypeSetFactory);
            ArchetypeSet ArchetypeSetFactory()
            {
                return new ArchetypeSet(type);
            }

            var archetype = set.Archetypes.FirstOrDefault(x => x.ComponentTypes.SetEquals(componentTypes));
            if (archetype == null)
            {
                archetype = new Archetype(componentTypes, _size);
                set.Archetypes.Add(archetype);
            }

            object?[] archetypeComponents = archetype.Components.GetOrAdd(type, ComponentsFactory);
            object?[] ComponentsFactory()
            {
                return new object?[_size];
            }

            archetypeComponents[entity] = component;
        }

        return entity;
    }

    private int NewEntity()
    {
        return Interlocked.Increment(ref _lastEntity);
    }
}