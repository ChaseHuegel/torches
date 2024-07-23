namespace Library.Collections;

public readonly struct Whitelist<T>(T[] values) : IFilter<T>
{
    private readonly T[] _values = values;

    public readonly bool Allowed(T value)
    {
        return _values.Contains(value);
    }

    public T[] Filter(T[] values)
    {
        return _values;
    }
}