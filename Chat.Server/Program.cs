using Chat.Server;
using Library.Configuration;
using Library.Configuration.Localization;
using Library.IO;
using Library.Serialization;
using Networking;
using Networking.LowLevel;
using Networking.Messaging;
using Networking.Serialization;
using Networking.Services;
using Packets.Chat;
using SmartFormat.Core.Extensions;
using SmartFormat.Utilities;
using Swordfish.Library.IO;

internal static class Program
{
    private static Container Container { get; } = new();

    private static ILoggerFactory _loggerFactory { get; } = LoggerFactory.Create(builder => builder.AddConsole());
    private static ILogger CreateLogger(Request request)
    {
        return _loggerFactory.CreateLogger(request.Parent.ImplementationType);
    }

    private static async Task Main(string[] args)
    {
        SetupContainer();
        SetupLocalization();

        var application = Container.Resolve<Application>();
        await application.Run();

        Container.Dispose();
    }

    private static void SetupContainer()
    {
        Container.RegisterMany<LengthDelimitedTcpService>(Reuse.Singleton);
        Container.Register<IParser, DirectParser>(Reuse.Singleton);
        Container.Register<IDataProducer, DataProducer>(setup: Setup.With(trackDisposableTransient: true), made: Parameters.Of.Type<IParser>().Type<IDataReceiver[]>());

        Container.RegisterMany<ChatPacketSerializer>(Reuse.Singleton);
        Container.Register<IMessageConsumer<ChatPacket>, PacketConsumer<ChatPacket>>(Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));
        Container.Register<IMessageProducer<ChatPacket>, MessageProducer<ChatPacket>>(Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));

        Container.RegisterMany<TextPacketSerializer>(Reuse.Singleton);
        Container.Register<IMessageConsumer<TextPacket>, PacketConsumer<TextPacket>>(Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));
        Container.Register<IMessageProducer<TextPacket>, MessageProducer<TextPacket>>(Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));

        Container.Register<Application>(Reuse.Singleton);
        Container.Register<ChatServer>(Reuse.Singleton);

        Container.Register<ILogger>(Made.Of(() => CreateLogger(Arg.Index<Request>(0)), request => request));

        Container.Register<IFileService, FileService>(Reuse.Singleton);
        Container.Register<IFileParser, LanguageParser>(Reuse.Singleton);
        Container.Register<ConfigurationProvider>(Reuse.Singleton);

        Container.Register<ILocalizationProvider, LocalizationService>(Reuse.Singleton);
        Container.Register<IFormatter, LocalizationFormatter>();
        Container.RegisterDelegate<IReadOnlyCollection<Language>>(static () => Container.Resolve<ConfigurationProvider>().GetLanguages());

        Container.Register<SessionService>(Reuse.Singleton);

        try
        {
            Container.ValidateAndThrow();
        }
        catch (ContainerException ex)
        {
            foreach (ContainerException? collectedException in ex.CollectedExceptions)
            {
                Console.WriteLine("Exception: " + collectedException + "\n");
            }
            Environment.Exit(1);
        }
    }

    private static void SetupLocalization()
    {
        Smart.Default.Settings.Localization.LocalizationProvider = Container.Resolve<ILocalizationProvider>();

        foreach (var formatter in Container.ResolveMany<IFormatter>())
        {
            Smart.Default.AddExtensions(formatter);
        }

        foreach (var source in Container.ResolveMany<ISource>())
        {
            Smart.Default.AddExtensions(source);
        }
    }
}