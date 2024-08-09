using System.Net;
using Chat.Server.Processors;
using Library.Services;
using Networking.Services;
using Swordfish.Library.IO;

namespace Chat.Server;

public class Application
{
    private readonly ILogger _logger;
    private readonly LengthDelimitedTcpServer _tcpService;
    private readonly IMessageEventProcessor[] _messageEventProcessors;
    private readonly CommandParser _commandParser;

    public Application(ILogger logger, LengthDelimitedTcpServer tcpService, IMessageEventProcessor[] messageEventProcessors, CommandParser commandParser)
    {
        _logger = logger;
        _tcpService = tcpService;
        _messageEventProcessors = messageEventProcessors;
        _commandParser = commandParser;
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

        while (await ProcessInputAsync(Console.ReadLine())) { }

        _logger.LogInformation("Closing server.");
    }

    private async Task<bool> ProcessInputAsync(string? input)
    {
        if (input == null)
        {
            return false;
        }

        CommandResult commandResult = await _commandParser.TryRunAsync(input);
        if (commandResult.State == CommandState.Failure)
        {
            _logger.LogWarning("Unknown command: \"{command}\".", commandResult.OriginalString);
        }

        return true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.LogError((Exception)e.ExceptionObject, "Unhandled exception.");
    }
}