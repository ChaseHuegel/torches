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
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public async Task Run()
    {
        _chatServer.Start();
        _tcpService.Start(new IPEndPoint(IPAddress.Loopback, 1234));
        _logger.LogInformation("Server started.");

        await Task.Delay(1000);
        _logger.LogInformation("Closing server.");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.LogError((Exception)e.ExceptionObject, "Unhandled exception.");
    }
}