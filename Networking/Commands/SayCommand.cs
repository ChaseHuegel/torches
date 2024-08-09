using Library.Collections;
using Library.Types;
using Networking.LowLevel;
using Networking.Messaging;
using Packets;
using Packets.Chat;
using Swordfish.Library.Collections;
using Swordfish.Library.IO;

namespace Networking.Commands;

public class SayCommand : Command
{
    public override string Option => "say";
    public override string Description => "Send a message to the local channel.";
    public override string ArgumentsHint => "<message>";

    private readonly IDataSender _sender;

    public SayCommand(IDataSender sender)
    {
        _sender = sender;
    }

    protected override Task<CommandState> InvokeAsync(ReadOnlyQueue<string> args)
    {
        string message = string.Join(' ', args.TakeAll());

        //  TODO need to introduce a PacketProducer<T> based on MessageProducer<T> that allows doing a simplified Send(new ChatPacket()).
        var packet = new Packet(PacketType.Chat, new ChatPacket(1, ChatChannel.Local, 0, message).Serialize());
        _sender.Send(packet.Serialize(), new All<Session>());

        return Task.FromResult(CommandState.Success);
    }
}