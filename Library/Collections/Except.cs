namespace Library.Collections;

public readonly struct Except<T>(T value) : IFilter<T>
{
    private readonly T _value = value;

    public readonly bool Allowed(T value)
    {
        if (_value == null)
        {
            return false;
        }

        return !_value.Equals(value);
    }

    public T[] Filter(T[] values)
    {
        return values.Except([_value]).ToArray();
    }
}