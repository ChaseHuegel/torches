using System.Reflection;
using System.Xml.Schema;
using Chat.Server;
using Chat.Server.Processors;
using Library.Configuration;
using Library.Configuration.Localization;
using Library.Events;
using Library.IO;
using Library.Serialization;
using Library.Util;
using Networking;
using Networking.LowLevel;
using Networking.Messaging;
using Networking.Serialization;
using Networking.Services;
using Packets.Chat;
using SmartFormat.Core.Extensions;
using SmartFormat.Utilities;
using Swordfish.Library.IO;

internal class Program
{
    private static readonly Container _container = new();
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger _logger;

    static Program()
    {
        _logger = CreateLogger<Program>();
    }

    private static async Task Main(string[] args)
    {
        SetupContainer();

        var application = _container.Resolve<Application>();
        await application.Run();

        _container.Dispose();
    }

    private static void SetupContainer()
    {
        _container.RegisterMany<LengthDelimitedTcpService>(Reuse.Singleton);
        _container.Register<IParser, DirectParser>(Reuse.Singleton);
        _container.Register<IDataProducer, DataProducer>(setup: Setup.With(trackDisposableTransient: true), made: Parameters.Of.Type<IParser>().Type<IDataReceiver[]>());

        _container.RegisterMany<ChatPacketSerializer>(Reuse.Singleton);
        _container.Register<IMessageConsumer<ChatPacket>, PacketConsumer<ChatPacket>>(Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));
        _container.Register<IMessageProducer<ChatPacket>, MessageProducer<ChatPacket>>(Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));

        _container.Register<IMessageEventProcessor, MessageEventProcessor<ChatPacket>>(Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.AppendNewImplementation);
        RegisterEventProcessors(Assembly.GetAssembly(typeof(Application))!, _container);

        _container.RegisterMany<TextPacketSerializer>(Reuse.Singleton);
        _container.Register<IMessageConsumer<TextPacket>, PacketConsumer<TextPacket>>(Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));
        _container.Register<IMessageProducer<TextPacket>, MessageProducer<TextPacket>>(Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));

        _container.Register<Application>(Reuse.Singleton);

        _container.Register<ILogger>(Made.Of(() => CreateLogger(Arg.Index<Request>(0)), request => request));

        _container.Register<IFileService, FileService>(Reuse.Singleton);
        _container.Register<IFileParser, LanguageParser>(Reuse.Singleton);
        _container.Register<ConfigurationProvider>(Reuse.Singleton);

        _container.RegisterDelegate<SmartFormatter>(SmartFormatterProvider.Resolve);
        _container.Register<ILocalizationProvider, LocalizationService>(Reuse.Singleton);
        _container.Register<IFormatter, LocalizationFormatter>(Reuse.Singleton);
        _container.RegisterDelegate<IReadOnlyCollection<Language>>(static () => _container.Resolve<ConfigurationProvider>().GetLanguages());

        _container.Register<SessionService>(Reuse.Singleton);

        KeyValuePair<ServiceInfo, ContainerException>[] errors = _container.Validate();
        if (errors.Length > 0)
        {
            foreach (KeyValuePair<ServiceInfo, ContainerException> error in errors)
            {
                _logger.LogError(error.Value, "There was an error validating a container (service: {service}).", error.Key);
            }
            Environment.Exit(1);
        }
    }

    private static void RegisterEventProcessors(Assembly assembly, Container container)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass)
            {
                continue;
            }

            if (type.GetInterface(typeof(IEventProcessor).FullName!) == null)
            {
                continue;
            }

            container.RegisterMany(serviceTypes: type.GetInterfaces(), implType: type, reuse: Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.AppendNewImplementation);
        }
    }

    private static ILogger CreateLogger(Request request)
    {
        return _loggerFactory.CreateLogger(request.Parent.ImplementationType);
    }

    public static ILogger CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }

    public static ILogger CreateLogger(Type type)
    {
        return _loggerFactory.CreateLogger(type);
    }
}