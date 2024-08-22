using System.Diagnostics;
using Arch.Core;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox.Mapper.Map;
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

    public struct Identification2 : IEntityComponent
    {
        public string Name;
        public string Tag;
    }

    [Test]
    public void BenchmarkWorldV3()
    {
        var world = new WorldV3(16);

        using (var elapsed = new ElapsedLogger("Created 100,000 named entity."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                world.Create(new Identification2() { Name = "Named entity" });
            }
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            world.Query((ref Identification2 id) => { });
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            world.Query((ref Identification2 id) => { });
        }
    }

    [Test]
    public void BenchmarkWorldV2()
    {
        var world = new WorldV2(100_001);

        using (var elapsed = new ElapsedLogger("Created 100,000 named entity."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                world.Create(new Identification2() { Name = "Named entity" });
            }
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            world.Query((ref Identification2 id) => { });
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            world.Query((ref Identification2 id) => { });
        }
    }

    public struct Identification : IComponent
    {
        public string Name;
        public string Tag;
    }

    [Test]
    public void BenchmarkArch()
    {
        var world = Arch.Core.World.Create();

        using (var elapsed = new ElapsedLogger("Created 100,000 named entity."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                world.Create(new IdentityComponent() { Name = "Named entity" });
            }
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var query = new QueryDescription().WithAll<IdentityComponent>();
            world.Query(in query, (ref IdentityComponent id) => { });
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var query = new QueryDescription().WithAll<IdentityComponent>();
            world.Query(in query, (ref IdentityComponent id) => { });
        }
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
                world.Add([new IdentityComponent { Name = "Named entity" }, null, null]);
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

    [Test]
    public void BenchmarkSimpleWorld()
    {
        var world = new SimpleWorld(100_001);

        using (var elapsed = new ElapsedLogger("Created 100,000 named entity."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                world.Create(new IdentityComponent { Name = "Named entity" });
            }
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var identityQuery = world.Query([typeof(IdentityComponent)]);
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var identityQuery = world.Query([typeof(IdentityComponent)]);
        }
    }

    [Test]
    public void BenchmarkWorld()
    {
        var world = new ECS.World(100_001);

        using (var elapsed = new ElapsedLogger("Created 100,000 named entity."))
        {
            for (int i = 0; i < 100_000; i++)
                world.Create(new IdentityComponent { Name = "Named entity" });
        }

        // using (var elapsed = new ElapsedLogger("Created named entity."))
        // {
        //     var namedEntity = world.Create(new IdentityComponent { Name = "Named entity" });
        // }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var identityQuery = world.Query([typeof(IdentityComponent)]);
        }

        using (var elapsed = new ElapsedLogger("Queried identity entity."))
        {
            var identityQuery = world.Query([typeof(IdentityComponent)]);
        }

        // using (var elapsed = new ElapsedLogger("Created location entity."))
        // {
        //     var locationEntity = world.Create(new PositionComponent { X = 50, Y = 50, Z = 50 });
        // }

        // using (var elapsed = new ElapsedLogger("Created 1000 living entity."))
        // {
        //     for (int i = 0; i < 1_000; i++)
        //         world.Create(new IdentityComponent { Name = "Living entity" }, new PositionComponent { X = 10, Y = 10, Z = 10 });
        // }

        // using (var elapsed = new ElapsedLogger("Created living entity."))
        // {
        //     var livingEntity = world.Create(new IdentityComponent { Name = "Living entity" }, new PositionComponent { X = 10, Y = 10, Z = 10 });
        // }

        // using (var elapsed = new ElapsedLogger("Queried identity entity."))
        // {
        //     var identityQuery = world.Query([typeof(IdentityComponent)]);
        // }

        // using (var elapsed = new ElapsedLogger("Queried identity entity."))
        // {
        //     var identityQuery = world.Query([typeof(IdentityComponent)]);
        // }

        // using (var elapsed = new ElapsedLogger("Queried location entity."))
        // {
        //     var positionQuery = world.Query([typeof(PositionComponent)]);
        // }

        // using (var elapsed = new ElapsedLogger("Queried living entity (ordered)."))
        // {
        //     var livingEntityQuery = world.Query([typeof(IdentityComponent), typeof(PositionComponent)]);
        // }

        // using (var elapsed = new ElapsedLogger("Queried living entity (unordered)."))
        // {
        //     var livingEntityQuery2 = world.Query([typeof(PositionComponent), typeof(IdentityComponent)]);
        // }

        // Assert.Multiple(() =>
        // {
        //     Assert.That(identityQuery, Has.Length.EqualTo(1));
        //     Assert.That(identityQuery[0], Is.EqualTo(1));

        //     Assert.That(positionQuery, Has.Length.EqualTo(1));
        //     Assert.That(positionQuery[0], Is.EqualTo(2));

        //     Assert.That(livingEntityQuery, Has.Length.EqualTo(1));
        //     Assert.That(livingEntityQuery[0], Is.EqualTo(3));

        //     Assert.That(livingEntityQuery2, Has.Length.EqualTo(1));
        //     Assert.That(livingEntityQuery2[0], Is.EqualTo(3));
        // });
    }
}