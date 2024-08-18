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

public class LengthDelimitedTcpClient : IDataReceiver, IDataSender
{
    public event EventHandler<DataEventArgs>? Received;

    private readonly SessionService _sessionService;
    private readonly ConcurrentDictionary<Session, NetworkStream> Connections = new();

    public LengthDelimitedTcpClient(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public void Connect(IPEndPoint endPoint)
    {
        TcpClient tcpClient = new();
        tcpClient.Connect(endPoint);
        NetworkStream stream = tcpClient.GetStream();
        Result<Session> sessionRequest = _sessionService.RequestNew();
        Connections.TryAdd(sessionRequest, stream);
        var listenWorker = new ThreadWorker(() => ListenToPeer(sessionRequest, stream), $"Peer.{sessionRequest.Value}.TCP.{endPoint.Port}");
        listenWorker.Start();
    }

    public Result Send(byte[] data, Session target)
    {
        if (!Connections.TryGetValue(target, out NetworkStream? peer))
        {
            return new Result(false, $"Unknown session: {target}.");
        }

        try
        {
            byte[] buffer = new byte[4 + data.Length];
            byte[] lengthDelimiter = BitConverter.GetBytes(buffer.Length);
            lengthDelimiter.CopyTo(buffer, 0);
            data.CopyTo(buffer, lengthDelimiter.Length);

            peer.Write(buffer);
            return new Result(true);
        }
        catch (Exception ex)
        {
            return new Result(false, ex.ToString());
        }
    }

    public Result Send(byte[] data, IFilter<Session> targetFilter)
    {
        bool success = true;
        StringBuilder? errorMessage = null;

        foreach (KeyValuePair<Session, NetworkStream> connection in Connections)
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

    private void ListenToPeer(Session session, NetworkStream stream)
    {
        try
        {
            while (_sessionService.Validate(session) && TryRecvFrame(session, stream)) { }
        }
        finally
        {
            stream.Dispose();
        }
    }

    private bool TryRecvFrame(Session session, NetworkStream stream)
    {
        //  Read the frame's length
        byte[] lengthBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            if (!TryReadNextByte(stream, out byte value))
            {
                return false;
            }

            lengthBytes[i] = value;
        }

        int length = BitConverter.ToInt32(lengthBytes, 0) - 4;

        //  Read the frame
        byte[] buffer = new byte[length];
        for (int i = 0; i < buffer.Length; i++)
        {
            if (!TryReadNextByte(stream, out byte value))
            {
                return false;
            }

            buffer[i] = value;
        }

        Received?.Invoke(this, new DataEventArgs(buffer, session));
        return true;
    }

    private static bool TryReadNextByte(NetworkStream stream, out byte value)
    {
        int read = stream.ReadByte();
        if (read == -1)
        {
            value = 0;
            return false;
        }

        value = (byte)read;
        return true;
    }
}