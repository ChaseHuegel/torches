﻿using Chat.Server.IO;
using Library;
using Library.DependencyInjection;
using Library.Serialization;
using Networking;
using Networking.LowLevel;
using Networking.Services;

namespace Chat.Server;

internal class Program : IDryIocModule
{
    public void Load(IContainer container)
    {
        container.Register<Application>();
        container.Register<IPacketProtocol, PacketProtocolRev1>(Reuse.Singleton);
        container.Register<ChatMessenger>(Reuse.Singleton);
        container.Register<SessionService>(Reuse.Singleton);
        container.Register<ILoginService, LoginService>(Reuse.Singleton);
        container.RegisterMany<TCPFrameServer>(Reuse.Singleton);
        container.Register<IParser, DirectParser>(Reuse.Singleton);
        container.Register<IDataProducer, DataProducer>(setup: Setup.With(trackDisposableTransient: true), made: Parameters.Of.Type<IParser>().Type<IDataReceiver[]>());
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