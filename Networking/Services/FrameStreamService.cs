using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.LowLevel;
using Swordfish.Library.Threading;

namespace Networking.Services;

public abstract class FrameStreamService(SessionService sessionService) : IDataReceiver, IDataSender
{
    public event EventHandler<DataEventArgs>? Received;

    private readonly SessionService _sessionService = sessionService;
    private readonly ConcurrentDictionary<Session, FrameStream> Connections = new();

    public Result Send(byte[] data, Session target)
    {
        if (!Connections.TryGetValue(target, out FrameStream? peer))
        {
            return new Result(false, $"Unknown session: {target}.");
        }

        return peer.WriteFrame(data);
    }

    public Result Send(byte[] data, IFilter<Session> targetFilter)
    {
        bool success = true;
        StringBuilder? errorMessage = null;

        foreach (KeyValuePair<Session, FrameStream> connection in Connections)
        {
            if (!targetFilter.Allowed(connection.Key))
            {
                continue;
            }

            Result send = Send(data, connection.Key);
            if (!send)
            {
                success = false;
                errorMessage ??= new StringBuilder();
                errorMessage.AppendLine($"Failed to send data to session: {connection.Key}, message: {send.Message}");
            }
        }

        return new Result(success, errorMessage?.ToString() ?? null);
    }

    protected Session AcceptPeer(FrameStream frameStream)
    {
        Session session = _sessionService.RequestNew();
        Connections.TryAdd(session, frameStream);

        var worker = new ThreadWorker(() => ListenToPeer(session, frameStream), $"{GetType().Name}.Peer.{session}");
        worker.Start();

        return session;
    }

    private void ListenToPeer(Session session, FrameStream frameStream)
    {
        try
        {
            while (_sessionService.Validate(session))
            {
                var readResult = frameStream.ReadFrame();
                if (!readResult)
                {
                    break;
                }

                Received?.Invoke(this, new DataEventArgs(readResult, session));
            }
        }
        finally
        {
            _sessionService.End(session);
            Connections.TryRemove(session, out _);
            frameStream.Dispose();
        }
    }
}