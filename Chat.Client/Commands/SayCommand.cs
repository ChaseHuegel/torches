using Library.Collections;
using Library.Types;
using Networking.LowLevel;
using Packets;
using Packets.Chat;
using Swordfish.Library.Collections;
using Swordfish.Library.IO;

namespace Chat.Client.Commands;

public class SayCommand(IDataSender sender) : Command
{
    public override string Option => "say";
    public override string Description => "Send a message to the local channel.";
    public override string ArgumentsHint => "<message>";

    private readonly IDataSender _sender = sender;

    protected override Task<CommandState> InvokeAsync(ReadOnlyQueue<string> args)
    {
        string message = string.Join(' ', args.TakeAll());

        if (string.IsNullOrWhiteSpace(message))
        {
            return Task.FromResult(CommandState.Failure);
        }

        //  TODO need to introduce a PacketProducer<T> based on MessageProducer<T> that allows doing a simplified Send(new ChatPacket()).
        var packet = new Packet(PacketType.Chat, 1, new ChatPacket(ChatChannel.Local, 0, message).Serialize());
        _sender.Send(packet.Serialize(), new All<Session>());

        return Task.FromResult(CommandState.Success);
    }
}