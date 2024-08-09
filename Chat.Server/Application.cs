using System.Net;
using Chat.Server.Processors;
using Library.Services;
using Networking.Services;

namespace Chat.Server;

public class Application
{
    private readonly ILogger _logger;
    private readonly LengthDelimitedTcpService _tcpService;
    private readonly IMessageEventProcessor[] _messageEventProcessors;

    public Application(ILogger logger, LengthDelimitedTcpService tcpService, IMessageEventProcessor[] messageEventProcessors)
    {
        _logger = logger;
        _tcpService = tcpService;
        _messageEventProcessors = messageEventProcessors;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public async Task Run()
    {
        _tcpService.Start(new IPEndPoint(IPAddress.Loopback, 1234));
        _logger.LogInformation("TCP service started.");

        foreach (IMessageEventProcessor processor in _messageEventProcessors)
        {
            processor.Start();
        }
        _logger.LogInformation("Started {count} message processors.", _messageEventProcessors.Length);

        await Task.Delay(2000);
        _logger.LogInformation("Closing server.");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.LogError((Exception)e.ExceptionObject, "Unhandled exception.");
    }
}