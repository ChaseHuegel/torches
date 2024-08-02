using Library.Collections;
using Library.Serialization;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.LowLevel;

namespace Networking.Messaging;

public class MessageProducer<T> : IMessageProducer<T>, IDisposable
{
    protected readonly ISerializer<T> _serializer;
    private IDataSender[] _senders;
    private bool _disposed;

    public MessageProducer(ISerializer<T> serializer, IDataSender[] senders)
    {
        List<IDataSender> matchingSenders = [];
        for (int i = 0; i < senders.Length; i++)
        {
            IDataSender dataProducer = senders[i];
            matchingSenders.Add(dataProducer);
        }

        _serializer = serializer;
        _senders = [.. matchingSenders];
    }

    public Result Send(T message, Session target)
    {
        byte[] data = _serializer.Serialize(message);

        bool success = true;
        for (int i = 0; i < _senders.Length; i++)
        {
            success &= _senders[i].Send(data, target);
        }

        return new Result(success);
    }

    public Result Send(T message, IFilter<Session> targetFilter)
    {
        byte[] data = _serializer.Serialize(message);

        bool success = true;
        for (int i = 0; i < _senders.Length; i++)
        {
            success &= _senders[i].Send(data, targetFilter);
        }

        return new Result(success);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _senders = null!;
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MessageProducer()
    {
        Dispose(false);
    }
}
