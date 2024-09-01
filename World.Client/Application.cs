using System.Net;
using Library.ECS;
using Networking.Events;
using Networking.Services;
using Swordfish.Library.IO;
using World.Client.Systems;

namespace World.Client;

internal class Application
{
    private readonly ILogger _logger;
    private readonly TCPFrameClient _tcpClient;
    private readonly IMessageEventProcessor[] _messageEventProcessors;
    private readonly CommandParser _commandParser;
    private readonly ECSContext _ecs;

    public Application(ILogger logger, TCPFrameClient tcpClient, IMessageEventProcessor[] messageEventProcessors, CommandParser commandParser, ECSContext ecs)
    {
        _logger = logger;
        _tcpClient = tcpClient;
        _messageEventProcessors = messageEventProcessors;
        _commandParser = commandParser;
        _ecs = ecs;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _ecs.AddSystem<PositionLogSystem>();
    }

    public async Task Run()
    {
        foreach (IMessageEventProcessor processor in _messageEventProcessors)
        {
            processor.Start();
            _logger.LogInformation("Started {type}.", processor.GetType());
        }
        _logger.LogInformation("Started {count} message processors.", _messageEventProcessors.Length);

        _tcpClient.Connect(new IPEndPoint(IPAddress.Loopback, 1235));
        _logger.LogInformation("TCP service connected.");

        while (await ProcessInputAsync(Console.ReadLine()))
        {
            await Task.Delay(50);
            _ecs.Tick();
        }

        _logger.LogInformation("Closing client.");
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

        _logger.LogInformation("Executed: {command}", commandResult.OriginalString);
        return true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.LogError((Exception)e.ExceptionObject, "Unhandled exception.");
    }
}