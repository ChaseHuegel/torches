using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.LowLevel;
using Packets;
using Packets.Chat;
using Swordfish.Library.Threading;

namespace Networking.Services;

public class LengthDelimitedTcpServer : IDataReceiver, IDataSender
{
    public event EventHandler<DataEventArgs>? Received;

    private readonly SessionService _sessionService;
    private readonly ConcurrentDictionary<Session, TcpClient> Connections = new();

    public LengthDelimitedTcpServer(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public void Start(IPEndPoint endPoint)
    {
        Task.Run(() => RunTcpServer(endPoint));
    }

    public Result Send(byte[] data, Session target)
    {
        if (!Connections.TryGetValue(target, out TcpClient? client))
        {
            return new Result(false, $"Unknown session: {target}.");
        }

        try
        {
            byte[] buffer = new byte[4 + data.Length];
            byte[] lengthDelimiter = BitConverter.GetBytes(buffer.Length);
            lengthDelimiter.CopyTo(buffer, 0);
            data.CopyTo(buffer, lengthDelimiter.Length);

            client.GetStream().Write(buffer);
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

        foreach (KeyValuePair<Session, TcpClient> connection in Connections)
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

    private async Task RunTcpServer(IPEndPoint endPoint)
    {
        TcpListener tcpServer = new(endPoint.Address, endPoint.Port);
        tcpServer.Start();

        while (true)
        {
            TcpClient client = await tcpServer.AcceptTcpClientAsync();
            Session session = _sessionService.RequestNew();
            Connections.TryAdd(session, client);
            var listenWorker = new ThreadWorker(() => ListenToPeer(session, client.GetStream()), $"TcpListener.Connection.{session}");
            listenWorker.Start();
        }
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