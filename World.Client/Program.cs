using Library;
using Library.DependencyInjection;
using Library.ECS;
using Library.Serialization;
using Networking;
using Networking.LowLevel;
using Networking.Services;

namespace World.Client;

internal class Program : IDryIocModule
{
    public void Load(IContainer container)
    {
        container.Register<Application>();
        container.Register<SessionService>(Reuse.Singleton);
        container.RegisterMany<TCPFrameClient>(Reuse.Singleton);
        container.Register<IParser, DirectParser>(Reuse.Singleton);
        container.Register<IDataProducer, DataProducer>(setup: Setup.With(trackDisposableTransient: true), made: Parameters.Of.Type<IParser>().Type<IDataReceiver[]>());

        container.Register<ECSContext>(Reuse.Singleton);
    }

    private static async Task Main(string[] args)
    {
        var engine = new Engine();
        engine.Start(args);

        var application = engine.Container.Resolve<Application>();
        await application.Run();

        engine.Dispose();
    }
}