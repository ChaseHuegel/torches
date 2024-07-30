using System.Net;
using Networking.Services;

namespace Chat.Server;

public class Application
{
    private readonly ChatServer _chatServer;
    private readonly LengthDelimitedTcpService _tcpService;
    private readonly ILogger _logger;

    public Application(ChatServer chatServer, LengthDelimitedTcpService tcpService, ILogger logger)
    {
        _chatServer = chatServer;
        _tcpService = tcpService;
        _logger = logger;
    }

    public async Task Run()
    {
        _logger.LogInformation("Starting services.");
        _chatServer.Start();
        _tcpService.Start(new IPEndPoint(IPAddress.Loopback, 1234));
        _logger.LogInformation("Services started.");

        await Task.Delay(5000);
        _logger.LogInformation("Closing server.");
    }
}