using DryIoc;
using Library.DependencyInjection;
using Networking.Events;
using Networking.Messaging;
using Packets.Chat;
using Packets.Entities;

namespace Networking.DependencyInjection;

public sealed class PacketRegistry : IDryIocModule
{
    public void Load(IContainer container)
    {
        RegisterPacketHandling(typeof(ChatPacket), container);
        RegisterPacketHandling(typeof(TextPacket), container);
        RegisterPacketHandling(typeof(EntityPacket), container);
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
}