
using Networking.Events;
using Swordfish;

namespace Networking.Messaging;

public class PacketAwaiter<T>
{
    private readonly IMessageConsumer<T> _consumer;
    private readonly SemaphoreSlim _semaphore = new(0, 1);
    private Optional<T> _result;

    public PacketAwaiter(IMessageConsumer<T> consumer)
    {
        _consumer = consumer;
        _consumer.NewMessage += OnNewMessage;
    }

    public async Task<T> WaitAsync()
    {
        await _semaphore.WaitAsync();
        return _result.Value;
    }

    private void OnNewMessage(object? sender, MessageEventArgs<T> e)
    {
        _consumer.NewMessage -= OnNewMessage;
        _result = new Optional<T>(e.Message);
        _semaphore.Release();
    }
}