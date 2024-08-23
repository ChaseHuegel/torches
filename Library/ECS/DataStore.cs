namespace Library.ECS;

public class DataStore
{
    private readonly int _chunkBitWidth;
    private readonly int _chunkLength;
    private readonly Dictionary<Type, ChunkedStore> _stores = [];

    private int _lastEntity = 0;

    public DataStore(byte chunkBitWidth = 16)
    {
        if (chunkBitWidth > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkBitWidth), "Chunk width can not exceed 30 bits.");
        }

        _chunkBitWidth = chunkBitWidth;
        _chunkLength = 1 << chunkBitWidth;
    }

    public int Create<T1>(T1 component1) where T1 : IDataComponent
    {
        int entity = NewEntity();
        AddComponent(entity, component1);
        return entity;
    }

    public int Create<T1, T2>(T1 component1, T2 component2)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        int entity = NewEntity();
        AddComponent(entity, component1);
        AddComponent(entity, component2);
        return entity;
    }

    public void AddComponent<T1>(int entity, T1 component1) where T1 : IDataComponent
    {
        ChunkedStore<T1> store1;
        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
        {
            store1 = new ChunkedStore<T1>();
            _stores.Add(typeof(T1), store1);
        }
        else
        {
            store1 = (ChunkedStore<T1>)store;
        }

        int chunkIndex = entity >> _chunkBitWidth;
        int localEntity = entity - (_chunkLength * chunkIndex);
        Chunk<T1> chunk;
        if (store1.Chunks.Count <= chunkIndex)
        {
            chunk = new Chunk<T1>(_chunkLength);
            store1.Chunks.Add(chunk);
        }
        else
        {
            chunk = store1.Chunks[chunkIndex];
        }

        chunk.Components[localEntity] = component1;
        chunk.Exists[localEntity] = true;
    }

    public void Query<T1>(ForEach<T1> forEach) where T1 : IDataComponent
    {
        ChunkedStore<T1> store1;
        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
        {
            return;
        }
        else
        {
            store1 = (ChunkedStore<T1>)store;
        }

        for (int chunkIndex = 0; chunkIndex < store1.Chunks.Count; chunkIndex++)
        {
            var chunk = store1.Chunks[chunkIndex];
            Parallel.ForEach(chunk.Components, ForEachEntity);
            void ForEachEntity(T1 c1, ParallelLoopState state, long componentIndex)
            {
                if (!chunk.Exists[componentIndex])
                {
                    return;
                }

                int entity = (int)componentIndex + (_chunkLength * chunkIndex);
                forEach(entity, ref c1);
            }
        }
    }

    public void Query<T1, T2>(ForEach<T1, T2> forEach)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        ChunkedStore<T1> store1;
        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
        {
            return;
        }
        else
        {
            store1 = (ChunkedStore<T1>)store;
        }

        ChunkedStore<T2> store2;
        if (!_stores.TryGetValue(typeof(T2), out ChunkedStore? storeB))
        {
            return;
        }
        else
        {
            store2 = (ChunkedStore<T2>)storeB;
        }

        for (int chunkIndex = 0; chunkIndex < store1.Chunks.Count; chunkIndex++)
        {
            if (store2.Chunks.Count <= chunkIndex)
            {
                return;
            }

            var chunk = store1.Chunks[chunkIndex];
            Parallel.ForEach(chunk.Components, ForEachEntity);
            void ForEachEntity(T1 c1, ParallelLoopState state, long componentIndex)
            {
                Chunk<T2> chunk2 = store2.Chunks[chunkIndex];
                if (!chunk.Exists[componentIndex] || !chunk2.Exists[componentIndex])
                {
                    return;
                }

                T2 c2 = chunk2.Components[componentIndex];
                int entity = (int)componentIndex + (_chunkLength * chunkIndex);
                forEach(entity, ref c1, ref c2);
            }
        }
    }

    private int NewEntity()
    {
        return Interlocked.Increment(ref _lastEntity);
    }
}