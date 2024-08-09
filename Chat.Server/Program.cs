using System.Reflection;
using System.Xml.Schema;
using Chat.Server;
using Chat.Server.Processors;
using Library.Configuration;
using Library.Configuration.Localization;
using Library.Configuration.Modding;
using Library.Events;
using Library.Serialization;
using Library.Serialization.Toml;
using Library.Services;
using Library.Util;
using Networking;
using Networking.LowLevel;
using Networking.Messaging;
using Networking.Services;
using Packets.Chat;
using SmartFormat.Core.Extensions;
using SmartFormat.Utilities;
using Swordfish.Library.IO;

internal class Program
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger _logger;

    private static IContainer _container;
    private static IContainer _modContainer;

    static Program()
    {
        _logger = CreateLogger<Program>();
        _container = new Container();
    }

    private static async Task Main(string[] args)
    {
        SetupCoreContainer();
        SetupModContainer();

        RegisterTomlMappers();

        var application = _container.Resolve<Application>();

        _modContainer.Resolve<IModLoader>().Load();

        await application.Run();
        _container.Dispose();
    }

    private static void SetupCoreContainer()
    {
        _container.RegisterMany<LengthDelimitedTcpService>(Reuse.Singleton);
        _container.Register<IParser, DirectParser>(Reuse.Singleton);
        _container.Register<IDataProducer, DataProducer>(setup: Setup.With(trackDisposableTransient: true), made: Parameters.Of.Type<IParser>().Type<IDataReceiver[]>());

        RegisterEventProcessors(Assembly.GetAssembly(typeof(Application))!, _container);

        RegisterSerializers(Assembly.GetAssembly(typeof(Application))!, _container);
        RegisterSerializers(Assembly.GetAssembly(typeof(ISerializer<>))!, _container);
        RegisterSerializers(Assembly.GetAssembly(typeof(IPacketSerializer<>))!, _container);

        RegisterPacketHandling(typeof(ChatPacket), _container);
        RegisterPacketHandling(typeof(TextPacket), _container);

        _container.Register<Application>(Reuse.Singleton);

        _container.Register<ILogger>(Made.Of(() => CreateLogger(Arg.Index<Request>(0)), request => request));

        _container.Register<ConfigurationProvider>(Reuse.Singleton);

        _container.Register<IFileService, FileService>(Reuse.Singleton);
        _container.Register<IFileParser, TomlParser<Language>>(Reuse.Singleton);
        _container.Register<IFileParser, TomlParser<ModOptions>>(Reuse.Singleton);
        _container.Register<IFileParser, TomlParser<ModManifest>>(Reuse.Singleton);

        _container.Register<ITomlMapper, PathTomlMapper>(Reuse.Singleton);

        _container.RegisterDelegate<SmartFormatter>(SmartFormatterProvider.Resolve);
        _container.Register<ILocalizationProvider, LocalizationService>(Reuse.Singleton);
        _container.Register<IFormatter, LocalizationFormatter>(Reuse.Singleton);
        _container.RegisterDelegate<IReadOnlyCollection<Language>>(static () => _container.Resolve<ConfigurationProvider>().GetLanguages().Select(languageFile => languageFile.Value).ToList());

        _container.Register<SessionService>(Reuse.Singleton);

        ValidateContainerOrDie(_container);
    }

    private static void SetupModContainer()
    {
        _modContainer = _container.With();
        _modContainer.Register<IModLoader, ModLoader>(Reuse.Singleton);
        ValidateContainerOrDie(_modContainer);
    }

    private static void ValidateContainerOrDie(IContainer container)
    {
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

    private static void RegisterPacketHandling(Type packetType, IContainer container)
    {
        Type packetConsumer = typeof(PacketConsumer<>).MakeGenericType([packetType]);
        container.RegisterMany(packetConsumer.GetInterfaces(), packetConsumer, reuse: Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));

        Type messageProducerInterface = typeof(IMessageProducer<>).MakeGenericType([packetType]);
        Type messageProducer = typeof(MessageProducer<>).MakeGenericType([packetType]);
        container.Register(messageProducerInterface, messageProducer, reuse: Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));

        Type messageEventProcessorInterface = typeof(IMessageEventProcessor);
        Type messageEventProcessor = typeof(MessageEventProcessor<>).MakeGenericType([packetType]);
        container.Register(messageEventProcessorInterface, messageEventProcessor, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.AppendNewImplementation);
    }

    private static void RegisterSerializers(Assembly assembly, IContainer container)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract)
            {
                continue;
            }

            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length == 0)
            {
                continue;
            }

            foreach (Type interfaceType in interfaces)
            {
                if (!interfaceType.IsGenericType)
                {
                    continue;
                }

                Type genericTypeDef = interfaceType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(ISerializer<>))
                {
                    container.RegisterMany(serviceTypes: interfaces, implType: type, reuse: Reuse.Singleton);
                    break;
                }
            }
        }
    }

    private static void RegisterEventProcessors(Assembly assembly, IContainer container)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract)
            {
                continue;
            }

            if (!type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventProcessor<>)))
            {
                continue;
            }

            container.RegisterMany(serviceTypes: type.GetInterfaces(), implType: type, reuse: Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.AppendNewImplementation);
        }
    }

    private static void RegisterTomlMappers()
    {
        foreach (ITomlMapper mapper in _container.ResolveMany<ITomlMapper>())
        {
            mapper.Register();
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