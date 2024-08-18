using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Networking.Services;

public class LengthDelimitedTcpClient(ILogger logger, SessionService sessionService) : FrameStreamService(sessionService)
{
    private readonly ILogger _logger = logger;

    public void Connect(IPEndPoint endPoint)
    {
        try
        {
            TcpClient tcpClient = new();

            tcpClient.Connect(endPoint);
            NetworkStream stream = tcpClient.GetStream();

            var frameStream = new FrameStream(stream);
            AcceptPeer(frameStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to a server at {endPoint}.", endPoint);
        }
    }
}