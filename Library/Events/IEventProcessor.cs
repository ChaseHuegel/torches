using Library.Util;

namespace Library.Events;

public interface IEventProcessor
{
}

public interface IEventProcessor<T> : IEventProcessor
{
    Result<EventBehavior> ProcessEvent(object? sender, T e);
}