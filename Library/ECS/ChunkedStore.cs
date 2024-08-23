namespace Library.ECS;

internal class ChunkedStore() { }

internal class ChunkedStore<T>() : ChunkedStore
{
    public readonly List<Chunk<T>> Chunks = [];
}