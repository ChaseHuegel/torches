using Library.Util;

namespace Library.Events;

public interface IEventProcessor<T>
{
    Result<EventBehavior> ProcessEvent(object? sender, T e);
}