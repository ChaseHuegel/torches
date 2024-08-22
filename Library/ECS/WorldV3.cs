namespace Library.ECS;

public class ArchetypeV3(int hash, Type[] types)
{
    public readonly int Hash = hash;
    public readonly Type[] Types = types;
}

public class ComponentStoreV3() { }

public class ComponentStoreV3<T>() : ComponentStoreV3
{
    public readonly List<ComponentChunk<T>> Chunks = [];
}

public class ComponentChunk() { }

public class ComponentChunk<T>(int size) : ComponentChunk
{
    public readonly int Size = size;
    public readonly T[] Components = new T[size];
    public readonly bool[] Exists = new bool[size];
}

public class WorldV3
{
    private int _lastEntity = 0;
    private readonly int _chunkBitWidth;
    private readonly int _chunkLength;

    public Dictionary<int, ArchetypeV3> ArchetypeMap = [];
    public Dictionary<Type, ComponentStoreV3> Stores = [];

    public WorldV3(byte chunkBitWidth = 16)
    {
        if (chunkBitWidth > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkBitWidth), "Chunk width can not exceed 30 bits.");
        }

        _chunkBitWidth = chunkBitWidth;
        _chunkLength = 1 << chunkBitWidth;
    }

    public void Query<T1>(ForEach<T1> forEach) where T1 : IEntityComponent
    {
        ComponentStoreV3<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStoreV3? store))
        {
            return;
        }
        else
        {
            store1 = (ComponentStoreV3<T1>)store;
        }

        Parallel.ForEach(store1.Chunks, ForEachChunk);
        void ForEachChunk(ComponentChunk<T1> chunk, ParallelLoopState state, long chunkIndex)
        {
            Parallel.ForEach(chunk.Components, ForEachEntity);
            void ForEachEntity(T1 c1, ParallelLoopState state, long componentIndex)
            {
                if (!chunk.Exists[componentIndex])
                {
                    return;
                }

                forEach(ref c1);
            }
        }
    }

    public void Query<T1, T2>(ForEach<T1, T2> forEach)
        where T1 : IEntityComponent
        where T2 : IEntityComponent
    {
        ComponentStoreV3<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStoreV3? store))
        {
            return;
        }
        else
        {
            store1 = (ComponentStoreV3<T1>)store;
        }

        ComponentStoreV3<T2> store2;
        if (!Stores.TryGetValue(typeof(T2), out ComponentStoreV3? storeB))
        {
            return;
        }
        else
        {
            store2 = (ComponentStoreV3<T2>)storeB;
        }

        Parallel.ForEach(store1.Chunks, ForEachChunk);
        void ForEachChunk(ComponentChunk<T1> chunk, ParallelLoopState state, long chunkIndex)
        {
            if (store2.Chunks.Count <= chunkIndex)
            {
                return;
            }

            Parallel.ForEach(chunk.Components, ForEachEntity);
            void ForEachEntity(T1 c1, ParallelLoopState state, long componentIndex)
            {
                ComponentChunk<T2> chunk2 = store2.Chunks[(int)chunkIndex];
                if (!chunk.Exists[componentIndex] || !chunk2.Exists[componentIndex])
                {
                    return;
                }

                T2 c2 = chunk2.Components[componentIndex];
                forEach(ref c1, ref c2);
            }
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
        ComponentStoreV3<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStoreV3? store))
        {
            store1 = new ComponentStoreV3<T1>();
            Stores.Add(typeof(T1), store1);
        }
        else
        {
            store1 = (ComponentStoreV3<T1>)store;
        }

        int chunkIndex = entity >> _chunkBitWidth;
        entity -= _chunkLength * chunkIndex;
        ComponentChunk<T1> chunk;
        if (store1.Chunks.Count <= chunkIndex)
        {
            chunk = new ComponentChunk<T1>(_chunkLength);
            store1.Chunks.Add(chunk);
        }
        else
        {
            chunk = store1.Chunks[chunkIndex];
        }

        chunk.Components[entity] = component1;
        chunk.Exists[entity] = true;
    }

    private int NewEntity()
    {
        return Interlocked.Increment(ref _lastEntity);
    }
}