using System.Runtime.CompilerServices;

namespace Library.ECS;

public class DataStore
{
    private readonly int _chunkBitWidth;
    private readonly int _chunkSize;
    private readonly Dictionary<Type, ChunkedStore> _stores = [];   //  TODO not thread safe

    private int _lastEntity = 0;

    public DataStore(byte chunkBitWidth = 16)
    {
        if (chunkBitWidth > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkBitWidth), "Chunk width can not exceed 30 bits.");
        }

        _chunkBitWidth = chunkBitWidth;
        _chunkSize = 1 << chunkBitWidth;
    }

    public int Create<T1>(T1 component1) where T1 : IDataComponent
    {
        int entity = NewEntity();
        (int chunkIndex, int localEntity) = ToChunkSpace(entity);
        SetAt(chunkIndex, localEntity, component1, true);
        return entity;
    }

    public int Create<T1, T2>(T1 component1, T2 component2)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        int entity = NewEntity();
        (int chunkIndex, int localEntity) = ToChunkSpace(entity);
        SetAt(chunkIndex, localEntity, component1, true);
        SetAt(chunkIndex, localEntity, component2, true);
        return entity;
    }

    public void Delete(int entity)
    {
        (int chunkIndex, int localEntity) = ToChunkSpace(entity);
        foreach (ChunkedStore store in _stores.Values)
        {
            store.SetAt(chunkIndex, localEntity, false);
        }
    }

    public void Add<T1>(int entity, T1 component1) where T1 : IDataComponent
    {
        ChunkedStore<T1> store1;
        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
        {
            store1 = new ChunkedStore<T1>(_chunkSize);
            _stores.Add(typeof(T1), store1);
        }
        else
        {
            store1 = (ChunkedStore<T1>)store;
        }

        (int chunkIndex, int localEntity) = ToChunkSpace(entity);
        store1.SetAt(chunkIndex, localEntity, component1, true);
    }

    public void Add<T1, T2>(int entity, T1 component1, T2 component2)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        ChunkedStore<T1> store1;
        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
        {
            store1 = new ChunkedStore<T1>(_chunkSize);
            _stores.Add(typeof(T1), store1);
        }
        else
        {
            store1 = (ChunkedStore<T1>)store;
        }

        ChunkedStore<T2> store2;
        if (!_stores.TryGetValue(typeof(T2), out ChunkedStore? storeB))
        {
            store2 = new ChunkedStore<T2>(_chunkSize);
            _stores.Add(typeof(T2), store2);
        }
        else
        {
            store2 = (ChunkedStore<T2>)storeB;
        }

        (int chunkIndex, int localEntity) = ToChunkSpace(entity);
        store1.SetAt(chunkIndex, localEntity, component1, true);
        store2.SetAt(chunkIndex, localEntity, component2, true);
    }

    public bool Remove<T1>(int entity) where T1 : IDataComponent
    {
        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
        {
            return false;
        }

        (int chunkIndex, int localEntity) = ToChunkSpace(entity);
        store.SetAt(chunkIndex, localEntity, false);
        return true;
    }

    public bool Remove<T1, T2>(int entity)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
        {
            return false;
        }

        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? storeB))
        {
            return false;
        }

        (int chunkIndex, int localEntity) = ToChunkSpace(entity);
        store.SetAt(chunkIndex, localEntity, false);
        storeB.SetAt(chunkIndex, localEntity, false);
        return true;
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

                int entity = ToGlobalSpace((int)componentIndex, chunkIndex);
                forEach(entity, ref c1);
                chunk.Components[componentIndex] = c1;  //  TODO use a buffer to apply changes?
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
                int entity = ToGlobalSpace((int)componentIndex, chunkIndex);
                forEach(entity, ref c1, ref c2);
                chunk.Components[componentIndex] = c1;  //  TODO use a buffer to apply changes?
                chunk2.Components[componentIndex] = c2;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NewEntity()
    {
        //  TODO recycle entities
        return Interlocked.Increment(ref _lastEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int chunkIndex, int localEntity) ToChunkSpace(int entity)
    {
        int chunkIndex = entity >> _chunkBitWidth;
        int localEntity = entity - (_chunkSize * chunkIndex);
        return (chunkIndex, localEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ToGlobalSpace(int chunkIndex, int localEntity)
    {
        return localEntity + (_chunkSize * chunkIndex);
    }

    private void SetAt<T1>(int chunkIndex, int localEntity, T1 component1, bool exists) where T1 : IDataComponent
    {
        ChunkedStore<T1> store1;
        if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
        {
            store1 = new ChunkedStore<T1>(_chunkSize);
            _stores.Add(typeof(T1), store1);
        }
        else
        {
            store1 = (ChunkedStore<T1>)store;
        }

        store1.SetAt(chunkIndex, localEntity, component1, exists);
    }
}