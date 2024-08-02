using Library.Events;
using Library.Util;
using Networking.Events;
using Networking.Messaging;

namespace Chat.Server.Processors;

public class MessageEventProcessor<TMessage> : IMessageEventProcessor
{
    private readonly ILogger _logger;
    private readonly IMessageConsumer<TMessage> _consumer;
    private readonly IEventProcessor<MessageEventArgs<TMessage>>[] _processors;

    public MessageEventProcessor(ILogger logger, IMessageConsumer<TMessage> consumer, IEventProcessor<MessageEventArgs<TMessage>>[] processors)
    {
        _logger = logger;
        _consumer = consumer;
        _processors = processors;
    }

    public void Start()
    {
        _consumer.NewMessage += OnNewMessage;
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