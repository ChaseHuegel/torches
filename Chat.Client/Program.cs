using System.Reflection;
using System.Windows.Input;
using System.Xml.Schema;
using Chat.Client;
using Library.Configuration;
using Library.Configuration.Localization;
using Library.Configuration.Modding;
using Library.Events;
using Library.Serialization;
using Library.Serialization.Toml;
using Library.Services;
using Library.Util;
using Networking;
using Networking.Commands;
using Networking.Events;
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

    static Program()
    {
        _logger = CreateLogger<Program>();

        IContainer coreContainer = SetupCoreContainer();
        RegisterTomlMappers(coreContainer);

        IContainer modulesContainer = SetupModulesContainer(coreContainer);
        RegisterTomlMappers(modulesContainer);

        _container = modulesContainer;
    }

    private static async Task Main(string[] args)
    {
        var application = _container.Resolve<Application>();
        await application.Run();
        _container.Dispose();
    }

    private static IContainer SetupCoreContainer()
    {
        IContainer container = new Container();
        container.RegisterMany<TCPFrameClient>(Reuse.Singleton);

        container.RegisterInstance<TextWriter>(Console.Out);

        container.Register<IParser, DirectParser>(Reuse.Singleton);
        container.Register<IDataProducer, DataProducer>(setup: Setup.With(trackDisposableTransient: true), made: Parameters.Of.Type<IParser>().Type<IDataReceiver[]>());

        container.Register<IModulesLoader, ModulesLoader>(Reuse.Singleton);

        container.Register<ILogger>(Made.Of(() => CreateLogger(Arg.Index<Request>(0)), request => request));

        container.Register<ConfigurationProvider>(Reuse.Singleton);

        container.Register<IFileService, FileService>(Reuse.Singleton);
        container.Register<IFileParser, TomlParser<Language>>(Reuse.Singleton);
        container.Register<IFileParser, TomlParser<ModuleOptions>>(Reuse.Singleton);
        container.Register<IFileParser, TomlParser<ModuleManfiest>>(Reuse.Singleton);

        container.Register<ITomlMapper, PathTomlMapper>(Reuse.Singleton);

        container.RegisterDelegate<SmartFormatter>(SmartFormatterProvider.Resolve);
        container.Register<ILocalizationProvider, LocalizationService>(Reuse.Singleton);
        container.Register<IFormatter, LocalizationFormatter>(Reuse.Singleton);
        container.RegisterDelegate<IReadOnlyCollection<Language>>(() => container.Resolve<ConfigurationProvider>().GetLanguages().Select(languageFile => languageFile.Value).ToList());

        container.Register<SessionService>(Reuse.Singleton);

        ValidateContainerOrDie(container);
        return container;
    }

    private static IContainer SetupModulesContainer(IContainer parentContainer)
    {
        IContainer container = parentContainer.With();
        container.Resolve<IModulesLoader>().Load(HookCallback);

        void HookCallback(Assembly assembly)
        {
            RegisterEventProcessors(assembly, container);
            RegisterSerializers(assembly, container);
        }

        // RegisterPacketHandling(typeof(ChatPacket), container);
        RegisterPacketHandling(typeof(TextPacket), container);

        container.Register<Application>(Reuse.Singleton);
        container.Register<CommandParser>(Reuse.Singleton, made: Made.Of(() => new CommandParser(Arg.Index<char>(0), Arg.Of<Command[]>()), _ => '\0'));
        container.Register<Command, SayCommand>(Reuse.Singleton);
        container.Register<Command, ExitCommand>(Reuse.Singleton);

        ValidateContainerOrDie(container);
        return container;
    }

    private static void ValidateContainerOrDie(IContainer container)
    {
        KeyValuePair<ServiceInfo, ContainerException>[] errors = container.Validate();
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

    private static void RegisterTomlMappers(IContainer container)
    {
        foreach (ITomlMapper mapper in container.ResolveMany<ITomlMapper>())
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