namespace Library.ECS;

public class ArchetypeV2(int hash, Type[] types)
{
    public readonly int Hash = hash;
    public readonly Type[] Types = types;
}

public class ComponentStore() { }

public class ComponentStore<T>(int size) : ComponentStore
{
    public readonly int Size = size;
    public readonly T[] Components = new T[size];
    public readonly bool[] Exists = new bool[size];
}

public interface IEntityComponent
{
}

public delegate void ForEach<T1>(ref T1 component1) where T1 : IEntityComponent;
public delegate void ForEach<T1, T2>(ref T1 component1, ref T2 component2) where T1 : IEntityComponent where T2 : IEntityComponent;

public class WorldV2(int size)
{
    private int _lastEntity = 0;
    private readonly int _size = size;

    public Dictionary<int, ArchetypeV2> ArchetypeMap = [];
    public Dictionary<Type, ComponentStore> Stores = [];

    public void Query<T1>(ForEach<T1> forEach) where T1 : IEntityComponent
    {
        ComponentStore<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStore? store))
        {
            return;
        }
        else
        {
            store1 = (ComponentStore<T1>)store;
        }

        Parallel.ForEach(store1.Components, InvokeForEach);
        void InvokeForEach(T1 c1, ParallelLoopState state, long index)
        {
            if (!store1.Exists[index])
            {
                return;
            }

            forEach(ref c1);
        }
    }

    public void Query<T1, T2>(ForEach<T1, T2> forEach)
        where T1 : IEntityComponent
        where T2 : IEntityComponent
    {
        ComponentStore<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStore? store))
        {
            return;
        }
        else
        {
            store1 = (ComponentStore<T1>)store;
        }

        ComponentStore<T2> store2;
        if (!Stores.TryGetValue(typeof(T2), out ComponentStore? storeB))
        {
            return;
        }
        else
        {
            store2 = (ComponentStore<T2>)storeB;
        }

        Parallel.ForEach(store1.Components, InvokeForEach);
        void InvokeForEach(T1 c1, ParallelLoopState state, long index)
        {
            if (!store1.Exists[index] || !store2.Exists[index])
            {
                return;
            }

            T2 c2 = store2.Components[index];
            forEach(ref c1, ref c2);
        }
    }

    public int Create<T1>(T1 component1) where T1 : IEntityComponent
    {
        int entity = NewEntity();
        AddComponent(entity, component1);
        return entity;
    }

    public int Create<T1, T2>(T1 component1, T2 component2)
        where T1 : IEntityComponent
        where T2 : IEntityComponent
    {
        int entity = NewEntity();
        AddComponent(entity, component1);
        AddComponent(entity, component2);
        return entity;
    }

    public void AddComponent<T1>(int entity, T1 component1) where T1 : IEntityComponent
    {
        ComponentStore<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStore? store))
        {
            store1 = new ComponentStore<T1>(_size);
            Stores.Add(typeof(T1), store1);
        }
        else
        {
            store1 = (ComponentStore<T1>)store;
        }

        store1.Components[entity] = component1;
        store1.Exists[entity] = true;
    }

    private int NewEntity()
    {
        return Interlocked.Increment(ref _lastEntity);
    }
}