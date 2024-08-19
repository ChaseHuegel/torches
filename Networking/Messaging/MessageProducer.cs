using System.Text;
using Library.Collections;
using Library.Serialization;
using Library.Types;
using Library.Util;
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
        StringBuilder? errorMessage = null;
        for (int i = 0; i < _senders.Length; i++)
        {
            Result sendResult = _senders[i].Send(data, target);
            success &= sendResult.Success;
            if (!success)
            {
                errorMessage ??= new StringBuilder();
                errorMessage.AppendLine(sendResult.Message);
            }
        }

        return new Result(success, errorMessage?.ToString() ?? null);
    }

    public Result Send(T message, IFilter<Session> targetFilter)
    {
        byte[] data = _serializer.Serialize(message);

        bool success = true;
        StringBuilder? errorMessage = null;
        for (int i = 0; i < _senders.Length; i++)
        {
            Result sendResult = _senders[i].Send(data, targetFilter);
            success &= sendResult.Success;
            if (!success)
            {
                errorMessage ??= new StringBuilder();
                errorMessage.AppendLine(sendResult.Message);
            }
        }

        return new Result(success, errorMessage?.ToString() ?? null);
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
