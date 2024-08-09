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

//  TODO make this a proper service instead of hardcoded test stuff
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
        Task.Run(() => RunTcpClient(endPoint));
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
            var listenWorker = new ThreadWorker(() => ListenToClient(client), $"TcpListener.Connection.{session}");
            listenWorker.Start();

            Packet[] packets = [
                new(PacketType.Chat, new ChatPacket(1, ChatChannel.Local, 0, "The quick").Serialize()),
                new(PacketType.Chat, new ChatPacket(1, ChatChannel.Global, 0, "brown fox jumped").Serialize()),
                new(PacketType.Chat, new ChatPacket(1, ChatChannel.Help, 0, "over the fence.").Serialize()),
            ];

            foreach (Packet packet in packets)
            {
                Send(packet.Serialize(), session);
            }
        }
    }

    private void ListenToClient(TcpClient tcpClient)
    {
        NetworkStream stream = tcpClient.GetStream();

        int lengthToRead = -1;
        int dataBufferOffset = 0;
        byte[] dataBuffer = new byte[2048];
        byte[] readBuffer = new byte[256];
        try
        {
            while (true)
            {
                RecvNext(stream, ref lengthToRead, ref dataBufferOffset, dataBuffer, readBuffer);
            }
        }
        catch
        {
            tcpClient.Dispose();
        }
    }

    private async Task RunTcpClient(IPEndPoint endPoint)
    {
        TcpClient tcpClient = new();
        await tcpClient.ConnectAsync(endPoint);
        var listenWorker = new ThreadWorker(() => ListenToClient(tcpClient), $"TcpClient.{endPoint.Port}");
        listenWorker.Start();
    }

    private void RecvNext(NetworkStream stream, ref int lengthToRead, ref int dataBufferOffset, byte[] dataBuffer, byte[] readBuffer)
    {
        int bytesRead = stream.Read(readBuffer);
        if (bytesRead == 0)
        {
            return;
        }

        Array.Copy(readBuffer, 0, dataBuffer, dataBufferOffset, bytesRead);
        dataBufferOffset += bytesRead;

        if (lengthToRead == -1 && dataBufferOffset >= 4)
        {
            lengthToRead = BitConverter.ToInt32(dataBuffer, 0);
        }

        if (dataBufferOffset >= lengthToRead)
        {
            byte[] data = dataBuffer[4..lengthToRead];

            Array.Copy(dataBuffer, lengthToRead, dataBuffer, 0, dataBuffer.Length - lengthToRead);
            dataBufferOffset -= lengthToRead;
            lengthToRead = -1;

            Received?.Invoke(this, new DataEventArgs(data, _sessionService.Get(1)));
        }
    }
}