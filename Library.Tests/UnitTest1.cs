using System.Diagnostics;
using Friflo.Engine.ECS;
using Library.ECS;
using Swordfish.Library.Collections;

namespace Library.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    private readonly struct ElapsedLogger(string context) : IDisposable
    {
        private readonly string _context = context;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public readonly void Dispose()
        {
            _stopwatch.Stop();
            Console.WriteLine($"{_context} Elapsed: {_stopwatch.Elapsed.TotalMilliseconds}ms / {_stopwatch.Elapsed.TotalMicroseconds}μs / {_stopwatch.Elapsed.TotalMilliseconds * 1000000}ns");
        }
    }

    public struct Identification2 : IDataComponent
    {
        public string Name;
        public string Tag;
    }

    [Test]
    public void BenchmarkDataStore()
    {
        var world = new DataStore(16);

        using (var elapsed = new ElapsedLogger("Created 100,000 named entity."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                world.Create(new Identification2() { Name = "Named entity" });
            }
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            world.Query((int entity, ref Identification2 id) => { });
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            world.Query((int entity, ref Identification2 id) => { });
        }
    }

    public struct Identification : IComponent
    {
        public string Name;
        public string Tag;
    }

    [Test]
    public void BenchmarkFriflo()
    {
        var world = new Friflo.Engine.ECS.EntityStore();

        using (var elapsed = new ElapsedLogger("Created 100,000 named entity."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                var entity = world.CreateEntity();
                entity.AddComponent(new Identification { Name = "Named entity" });
            }
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var query = world.Query<Identification>();
            query.ForEachEntity((ref Identification id, Friflo.Engine.ECS.Entity entity) => { });
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var query = world.Query<Identification>();
            query.ForEachEntity((ref Identification id, Friflo.Engine.ECS.Entity entity) => { });
        }
    }

    [Test]
    public void BenchmarkChunkedDataStore()
    {
        var world = new ChunkedDataStore(100_001, 3);

        using (var elapsed = new ElapsedLogger("Created 100,000 named entity."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                world.Add([new Identification { Name = "Named entity" }, null, null]);
            }
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var entities = world.All();
            int[] matches = new int[entities.Length];
            int matchOffset = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                if (world.HasAt(entities[i], 0))
                {
                    matches[matchOffset++] = i;
                }
            }

            ArraySegment<int> identityQuery = new(matches, 0, matchOffset);
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var entities = world.All();
            int[] matches = new int[entities.Length];
            int matchOffset = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                if (world.HasAt(entities[i], 0))
                {
                    matches[matchOffset++] = i;
                }
            }

            ArraySegment<int> identityQuery = new(matches, 0, matchOffset);
        }
    }
}