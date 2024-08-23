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

    public struct IdentityComponent : IDataComponent, IComponent
    {
        public string Name;
        public string Tag;
    }

    public struct PositionComponent(float x, float y, float z) : IDataComponent, IComponent
    {
        public float X = x;
        public float Y = y;
        public float Z = z;
    }

    public struct PhysicsComponent : IDataComponent, IComponent;

    public class GravitySystem(DataStore store) : EntitySystem<PhysicsComponent, PositionComponent>(store)
    {
        protected override void OnTick(int entity, ref PhysicsComponent physics, ref PositionComponent position)
        {
            position.Y -= 9.8f;
        }
    }

    [Test]
    public void Benchmark_ECSContext_1System_3Components()
    {
        var ecs = new ECSContext();
        ecs.AddSystem<GravitySystem>();

        using (var elapsed = new ElapsedLogger("Created 100,000 entities."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                int entity = ecs.DataStore.Create(new PhysicsComponent(), new PositionComponent(i, 0, 0));
                ecs.DataStore.Add(entity, new IdentityComponent() { Name = $"Entity {entity} ({i})" });
            }
        }

        using (var elapsed = new ElapsedLogger("Queried entities."))
        {
            ecs.DataStore.Query((int entity, ref IdentityComponent id, ref PositionComponent position) => { });
        }

        using (var elapsed = new ElapsedLogger("Ticked entities."))
        {
            ecs.Tick();
        }

        using (var elapsed = new ElapsedLogger("Ticked entities."))
        {
            ecs.Tick();
        }

        using (var elapsed = new ElapsedLogger("Ticked entities."))
        {
            ecs.Tick();
        }

        using (var elapsed = new ElapsedLogger("Queried entities."))
        {
            ecs.DataStore.Query((int entity, ref IdentityComponent id, ref PositionComponent position) => { });
        }
    }

    [Test]
    public void Benchmark_Friflo_1System_3Components()
    {
        var world = new Friflo.Engine.ECS.EntityStore();

        using (var elapsed = new ElapsedLogger("Created 100,000 entities."))
        {
            for (int i = 0; i < 100_000; i++)
            {
                var entity = world.CreateEntity();
                entity.AddComponent(new IdentityComponent { Name = $"Entity {entity.Id} ({i})" });
                entity.AddComponent(new PhysicsComponent());
                entity.AddComponent(new PositionComponent(i, 0, 0));
            }
        }

        using (var elapsed = new ElapsedLogger("Queried entities."))
        {
            var query = world.Query<IdentityComponent, PositionComponent>();
            query.ForEachEntity((ref IdentityComponent id, ref PositionComponent loc, Friflo.Engine.ECS.Entity entity) => { });
        }

        using (var elapsed = new ElapsedLogger("Ticked entities."))
        {
            var query = world.Query<PhysicsComponent, PositionComponent>();
            query.ForEachEntity((ref PhysicsComponent physics, ref PositionComponent loc, Friflo.Engine.ECS.Entity entity) => { loc.Y -= 0.9f; });
        }

        using (var elapsed = new ElapsedLogger("Ticked entities."))
        {
            var query = world.Query<PhysicsComponent, PositionComponent>();
            query.ForEachEntity((ref PhysicsComponent physics, ref PositionComponent loc, Friflo.Engine.ECS.Entity entity) => { loc.Y -= 0.9f; });
        }

        using (var elapsed = new ElapsedLogger("Ticked entities."))
        {
            var query = world.Query<PhysicsComponent, PositionComponent>();
            query.ForEachEntity((ref PhysicsComponent physics, ref PositionComponent loc, Friflo.Engine.ECS.Entity entity) => { loc.Y -= 0.9f; });
        }

        using (var elapsed = new ElapsedLogger("Queried entities."))
        {
            var query = world.Query<IdentityComponent, PositionComponent>();
            query.ForEachEntity((ref IdentityComponent id, ref PositionComponent loc, Friflo.Engine.ECS.Entity entity) => { });
        }
    }
}