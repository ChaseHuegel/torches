using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Library.ECS;

public class DataStore
{
    private readonly int _chunkBitWidth;
    private readonly int _chunkSize;
    private readonly Dictionary<Type, ChunkedStore> _stores = [];   //  TODO not thread safe
    private readonly object _chunkAndStoreLock = new();

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
        lock (_chunkAndStoreLock)
        {
            int entity = NewEntity();
            (int chunkIndex, int localEntity) = ToChunkSpace(entity);
            SetAt(chunkIndex, localEntity, component1, true);
            return entity;
        }
    }

    public int Create<T1, T2>(T1 component1, T2 component2)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        lock (_chunkAndStoreLock)
        {
            int entity = NewEntity();
            (int chunkIndex, int localEntity) = ToChunkSpace(entity);
            SetAt(chunkIndex, localEntity, component1, true);
            SetAt(chunkIndex, localEntity, component2, true);
            return entity;
        }
    }

    public void Delete(int entity)
    {
        lock (_chunkAndStoreLock)
        {
            (int chunkIndex, int localEntity) = ToChunkSpace(entity);
            foreach (ChunkedStore store in _stores.Values)
            {
                store.SetAt(chunkIndex, localEntity, false);
            }
        }
    }

    public void Add<T1>(int entity, T1 component1) where T1 : IDataComponent
    {
        lock (_chunkAndStoreLock)
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
    }

    public void Add<T1, T2>(int entity, T1 component1, T2 component2)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        lock (_chunkAndStoreLock)
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
    }

    public bool Remove<T1>(int entity) where T1 : IDataComponent
    {
        lock (_chunkAndStoreLock)
        {
            if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
            {
                return false;
            }

            (int chunkIndex, int localEntity) = ToChunkSpace(entity);
            store.SetAt(chunkIndex, localEntity, false);
            return true;
        }
    }

    public bool Remove<T1, T2>(int entity)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        lock (_chunkAndStoreLock)
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
    }

    public unsafe void Query<T1>(ForEach<T1> forEach) where T1 : IDataComponent
    {
        Span<Chunk<T1>> chunks;
        lock (_chunkAndStoreLock)
        {
            if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store))
            {
                return;
            }
            else
            {
                chunks = CollectionsMarshal.AsSpan(((ChunkedStore<T1>)store).Chunks);
            }
        }

        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            Chunk<T1> chunk = chunks[chunkIndex];
            for (int componentIndex = 0; componentIndex < chunk.Components.Length; componentIndex++)
            {
                if (!chunk.Exists[componentIndex])
                {
                    continue;
                }

                int entity = ToGlobalSpace(componentIndex, chunkIndex);
                T1 c1 = chunk.Components[componentIndex];

                forEach(entity, ref c1);

                chunk.Components[componentIndex] = c1;  //  TODO use a buffer to apply changes?
            }
        }
    }

    public void Query<T1, T2>(ForEach<T1, T2> forEach)
        where T1 : IDataComponent
        where T2 : IDataComponent
    {
        Span<Chunk<T1>> chunks1;
        Span<Chunk<T2>> chunks2;
        lock (_chunkAndStoreLock)
        {
            if (!_stores.TryGetValue(typeof(T1), out ChunkedStore? store1))
            {
                return;
            }
            else
            {
                chunks1 = CollectionsMarshal.AsSpan(((ChunkedStore<T1>)store1).Chunks);
            }

            if (!_stores.TryGetValue(typeof(T2), out ChunkedStore? store2))
            {
                return;
            }
            else
            {
                chunks2 = CollectionsMarshal.AsSpan(((ChunkedStore<T2>)store2).Chunks);
            }
        }

        int minChunks = Math.Min(chunks1.Length, chunks2.Length);
        for (int chunkIndex = 0; chunkIndex < minChunks; chunkIndex++)
        {
            Chunk<T1> chunk1 = chunks1[chunkIndex];
            for (int componentIndex = 0; componentIndex < chunk1.Components.Length; componentIndex++)
            {
                Chunk<T2> chunk2 = chunks2[chunkIndex];
                if (!chunk1.Exists[componentIndex] || !chunk2.Exists[componentIndex])
                {
                    continue;
                }

                int entity = ToGlobalSpace(componentIndex, chunkIndex);
                T1 c1 = chunk1.Components[componentIndex];
                T2 c2 = chunk2.Components[componentIndex];

                forEach(entity, ref c1, ref c2);

                chunk1.Components[componentIndex] = c1;  //  TODO use a buffer to apply changes?
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