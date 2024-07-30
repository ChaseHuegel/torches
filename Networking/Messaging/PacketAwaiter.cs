
using System.Runtime.CompilerServices;
using Networking.Events;

namespace Networking.Messaging;

public readonly struct PacketAwaiter<T>
{
    private readonly IMessageConsumer<T> _consumer;
    private readonly TaskCompletionSource<T> _taskCompletionSource;

    public PacketAwaiter(IMessageConsumer<T> consumer)
    {
        _taskCompletionSource = new TaskCompletionSource<T>();
        _consumer = consumer;
        _consumer.NewMessage += OnNewMessage;
    }

    private void OnNewMessage(object? sender, MessageEventArgs<T> e)
    {
        _consumer.NewMessage -= OnNewMessage;
        _taskCompletionSource.SetResult(e.Message);
    }

    public readonly TaskAwaiter<T> GetAwaiter()
    {
        return _taskCompletionSource.Task.GetAwaiter();
    }
}