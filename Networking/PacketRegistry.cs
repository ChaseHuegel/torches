using DryIoc;
using Library.DependencyInjection;
using Networking.Events;
using Networking.Messaging;
using Packets.Auth;
using Packets.Chat;
using Packets.Entities;
using Packets.World;

namespace Networking.DependencyInjection;

public sealed class PacketRegistry : IDryIocModule
{
    public void Load(IContainer container)
    {
        RegisterPacketHandling(typeof(ChatPacket), container);
        RegisterPacketHandling(typeof(TextPacket), container);
        RegisterPacketHandling(typeof(EntityPacket), container);
        RegisterPacketHandling(typeof(LoginRequestPacket), container);
        RegisterPacketHandling(typeof(LoginResponsePacket), container);
        RegisterPacketHandling(typeof(LogoutPacket), container);
        RegisterPacketHandling(typeof(JoinRequestPacket), container);
        RegisterPacketHandling(typeof(JoinResponsePacket), container);
        RegisterPacketHandling(typeof(CharacterPacket), container);
    }

    private static void RegisterPacketHandling(Type packetType, IContainer container)
    {
        Type packetConsumer = typeof(PacketConsumer<>).MakeGenericType([packetType]);
        container.RegisterMany([.. packetConsumer.GetInterfaces(), packetConsumer], packetConsumer, reuse: Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));

        Type messageProducerInterface = typeof(IMessageProducer<>).MakeGenericType([packetType]);
        Type messageProducer = typeof(MessageProducer<>).MakeGenericType([packetType]);
        container.Register(messageProducerInterface, messageProducer, reuse: Reuse.Singleton, setup: Setup.With(trackDisposableTransient: true));

        Type messageEventProcessorInterface = typeof(IMessageEventProcessor);
        Type messageEventProcessor = typeof(MessageEventProcessor<>).MakeGenericType([packetType]);
        container.Register(messageEventProcessorInterface, messageEventProcessor, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.AppendNewImplementation);
    }
}