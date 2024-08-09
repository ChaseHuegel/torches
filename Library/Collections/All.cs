namespace Library.Collections;

public readonly struct All<T>() : IFilter<T>
{
    public readonly bool Allowed(T value)
    {
        return true;
    }

    public T[] Filter(T[] values)
    {
        return values;
    }
}