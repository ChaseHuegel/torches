namespace Library.ECS;

public delegate void ForEachEntity<T1>(int entity, ref T1 component1) where T1 : IEntityComponent;
public delegate void ForEachEntity<T1, T2>(int entity, ref T1 component1, ref T2 component2) where T1 : IEntityComponent where T2 : IEntityComponent;

public class ComponentStoreV4() { }

public class ComponentStoreV4<T>() : ComponentStoreV4
{
    public readonly List<ComponentChunkV4<T>> Chunks = [];
}

public class ComponentChunkV4() { }

public class ComponentChunkV4<T>(int size) : ComponentChunkV4
{
    public readonly int Size = size;
    public readonly T[] Components = new T[size];
    public readonly bool[] Exists = new bool[size];
}

public class WorldV4
{
    private int _lastEntity = 0;
    private readonly int _chunkBitWidth;
    private readonly int _chunkLength;

    public Dictionary<Type, ComponentStoreV4> Stores = [];

    public WorldV4(byte chunkBitWidth = 16)
    {
        if (chunkBitWidth > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkBitWidth), "Chunk width can not exceed 30 bits.");
        }

        _chunkBitWidth = chunkBitWidth;
        _chunkLength = 1 << chunkBitWidth;
    }

    public void Query<T1>(ForEachEntity<T1> forEach) where T1 : IEntityComponent
    {
        ComponentStoreV4<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStoreV4? store))
        {
            return;
        }
        else
        {
            store1 = (ComponentStoreV4<T1>)store;
        }

        Parallel.ForEach(store1.Chunks, ForEachChunk);
        void ForEachChunk(ComponentChunkV4<T1> chunk, ParallelLoopState state, long chunkIndex)
        {
            Parallel.ForEach(chunk.Components, ForEachEntity);
            void ForEachEntity(T1 c1, ParallelLoopState state, long componentIndex)
            {
                if (!chunk.Exists[componentIndex])
                {
                    return;
                }

                int entity = (int)componentIndex + (_chunkLength * (int)chunkIndex);
                forEach(entity, ref c1);
            }
        }
    }

    public void Query<T1, T2>(ForEachEntity<T1, T2> forEach)
        where T1 : IEntityComponent
        where T2 : IEntityComponent
    {
        ComponentStoreV4<T2> store2;
        if (!Stores.TryGetValue(typeof(T2), out ComponentStoreV4? storeB))
        {
            return;
        }
        else
        {
            store2 = (ComponentStoreV4<T2>)storeB;
        }

        QueryInternal<T1>(ForEachChunk, ForEachEntity);

        bool ForEachChunk(ComponentChunkV4<T1> chunk, ParallelLoopState state, long chunkIndex)
        {
            if (store2.Chunks.Count <= chunkIndex)
            {
                return false;
            }

            return true;
        }

        void ForEachEntity(int entity, ref T1 c1)
        {
            int chunkIndex = entity >> _chunkBitWidth;
            int localEntity = entity - (_chunkLength * chunkIndex);

            ComponentChunkV4<T2> chunk2 = store2.Chunks[chunkIndex];
            if (!chunk2.Exists[localEntity])
            {
                return;
            }
            T2 c2 = chunk2.Components[localEntity];

            forEach(entity, ref c1, ref c2);
        }
    }

    private void QueryInternal<T1>(Func<ComponentChunkV4<T1>, ParallelLoopState, long, bool> forEachChunk, ForEachEntity<T1> forEachEntity) where T1 : IEntityComponent
    {
        ComponentStoreV4<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStoreV4? store))
        {
            return;
        }
        else
        {
            store1 = (ComponentStoreV4<T1>)store;
        }

        Parallel.ForEach(store1.Chunks, ForEachChunk);
        void ForEachChunk(ComponentChunkV4<T1> chunk, ParallelLoopState state, long chunkIndex)
        {
            if (!forEachChunk(chunk, state, chunkIndex))
            {
                return;
            }

            Parallel.ForEach(chunk.Components, ForEachEntity);
            void ForEachEntity(T1 c1, ParallelLoopState state, long componentIndex)
            {
                if (!chunk.Exists[componentIndex])
                {
                    return;
                }

                int entity = (int)componentIndex + (_chunkLength * (int)chunkIndex);
                forEachEntity(entity, ref c1);
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
        ComponentStoreV4<T1> store1;
        if (!Stores.TryGetValue(typeof(T1), out ComponentStoreV4? store))
        {
            store1 = new ComponentStoreV4<T1>();
            Stores.Add(typeof(T1), store1);
        }
        else
        {
            store1 = (ComponentStoreV4<T1>)store;
        }

        int chunkIndex = entity >> _chunkBitWidth;
        int localEntity = entity - (_chunkLength * chunkIndex);
        ComponentChunkV4<T1> chunk;
        if (store1.Chunks.Count <= chunkIndex)
        {
            chunk = new ComponentChunkV4<T1>(_chunkLength);
            store1.Chunks.Add(chunk);
        }
        else
        {
            chunk = store1.Chunks[chunkIndex];
        }

        chunk.Components[localEntity] = component1;
        chunk.Exists[localEntity] = true;
    }

    private int NewEntity()
    {
        return Interlocked.Increment(ref _lastEntity);
    }
}