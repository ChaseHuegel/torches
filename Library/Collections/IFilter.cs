namespace Library.Collections;

public interface IFilter<T>
{
    bool Allowed(T value);

    T[] Filter(T[] values);
}
