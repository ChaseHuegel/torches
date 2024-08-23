namespace Library.ECS;

public abstract class EntitySystem(DataStore store)
{
    protected readonly DataStore Store = store;

    public abstract void Tick();
}

public abstract class EntitySystem<T1>(DataStore store) : EntitySystem(store) where T1 : IDataComponent
{
    public override void Tick()
    {
        Store.Query<T1>(OnTick);
    }

    protected abstract void OnTick(int entity, ref T1 component1);
}

public abstract class EntitySystem<T1, T2>(DataStore store) : EntitySystem(store)
    where T1 : IDataComponent
    where T2 : IDataComponent
{
    public override void Tick()
    {
        Store.Query<T1, T2>(OnTick);
    }

    protected abstract void OnTick(int entity, ref T1 component1, ref T2 component2);
}