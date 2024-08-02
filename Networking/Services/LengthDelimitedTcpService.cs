using System.Net;
using System.Net.Sockets;
using Library.Collections;
using Library.Types;
using Library.Util;
using Networking.Events;
using Networking.LowLevel;
using Packets;
using Packets.Chat;

namespace Networking.Services;

//  TODO make this a proper service instead of hardcoded test stuff
public class LengthDelimitedTcpService : IDataReceiver, IDataSender
{
    public event EventHandler<DataEventArgs>? Received;

    private readonly SessionService _sessionService;

    public LengthDelimitedTcpService(SessionService sessionService)
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
        //  TODO implement
        return new Result(true);
    }

    public Result Send(byte[] data, IFilter<Session> targetFilter)
    {
        //  TODO implement
        return new Result(true);
    }

    private async Task RunTcpServer(IPEndPoint endPoint)
    {
        TcpListener tcpServer = new(endPoint.Address, endPoint.Port);
        tcpServer.Start();

        TcpClient client = await tcpServer.AcceptTcpClientAsync();
        NetworkStream stream = client.GetStream();

        Packet[] packets = [
            new(PacketType.Chat, new ChatPacket(1, ChatChannel.Local, 0, "The quick").Serialize()),
            new(PacketType.Chat, new ChatPacket(1, ChatChannel.Global, 0, "brown fox jumped").Serialize()),
            new(PacketType.Chat, new ChatPacket(1, ChatChannel.Help, 0, "over the fence.").Serialize()),
        ];

        foreach (Packet packet in packets)
        {
            byte[] packetBuffer = packet.Serialize();
            byte[] buffer = new byte[4 + packetBuffer.Length];
            byte[] lengthDelimiter = BitConverter.GetBytes(buffer.Length);
            lengthDelimiter.CopyTo(buffer, 0);
            packetBuffer.CopyTo(buffer, lengthDelimiter.Length);

            await stream.WriteAsync(buffer);
        }

        stream.Close();
    }

    private async Task RunTcpClient(IPEndPoint endPoint)
    {
        TcpClient tcpClient = new();
        await tcpClient.ConnectAsync(endPoint);

        Session serverSession = _sessionService.RequestNew().Value;

        NetworkStream stream = tcpClient.GetStream();

        int lengthToRead = -1;
        int dataBufferOffset = 0;
        byte[] dataBuffer = new byte[2048];
        byte[] readBuffer = new byte[256];
        try
        {
            while (tcpClient.Connected)
            {
                int bytesRead = await stream.ReadAsync(readBuffer);
                if (bytesRead == 0)
                {
                    stream.Close();
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

                    Received?.Invoke(this, new DataEventArgs(data, serverSession));
                }
            }
        }
        catch
        {
            tcpClient.Dispose();
        }
    }
}