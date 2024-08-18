using Library.DependencyInjection;
using Library.Serialization;
using Networking;
using Networking.LowLevel;
using Networking.Services;

namespace Chat.Client.DependencyInjection;

public class ContainerModule : IDryIocModule
{
    public void Load(IContainer container)
    {
        container.Register<SessionService>(Reuse.Singleton);
        container.RegisterMany<TCPFrameClient>(Reuse.Singleton);
        container.Register<IParser, DirectParser>(Reuse.Singleton);
        container.Register<IDataProducer, DataProducer>(setup: Setup.With(trackDisposableTransient: true), made: Parameters.Of.Type<IParser>().Type<IDataReceiver[]>());
    }
}