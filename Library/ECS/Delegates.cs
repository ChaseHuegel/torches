namespace Library.ECS;

public delegate void ForEach<T1>(int index, ref T1 component1) where T1 : IDataComponent;
public delegate void ForEach<T1, T2>(int index, ref T1 component1, ref T2 component2) where T1 : IDataComponent where T2 : IDataComponent;