using Library.Events;
using Library.Util;
using Microsoft.Extensions.Logging;
using Networking.Messaging;

namespace Networking.Events;

public sealed class MessageEventProcessor<TMessage>(
    ILogger logger,
    IMessageConsumer<TMessage> consumer,
    IEventProcessor<MessageEventArgs<TMessage>>[] processors
) : IMessageEventProcessor, IDisposable
{
    private readonly ILogger _logger = logger;
    private readonly IMessageConsumer<TMessage> _consumer = consumer;
    private readonly IEventProcessor<MessageEventArgs<TMessage>>[] _processors = processors;

    public void Start()
    {
        _consumer.NewMessage += OnNewMessage;
    }

    public void Dispose()
    {
        _consumer.NewMessage -= OnNewMessage;
    }

    private void OnNewMessage(object? sender, MessageEventArgs<TMessage> e)
    {
        for (int i = 0; i < _processors.Length; i++)
        {
            var processor = _processors[i];

            Result<EventBehavior> result = processor.ProcessEvent(sender, e);
            if (!result)
            {
                _logger.LogWarning("{processor} failed to process a {messageType}: {message}", processor.GetType(), typeof(TMessage), result.Message);
            }

            if (result == EventBehavior.Consume)
            {
                break;
            }
        }
    }
}