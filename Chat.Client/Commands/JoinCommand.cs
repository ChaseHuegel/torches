using Library.Collections;
using Library.Types;
using Networking.LowLevel;
using Networking.Messaging;
using Packets;
using Packets.Auth;
using Packets.World;
using Swordfish.Library.Collections;
using Swordfish.Library.IO;

namespace Chat.Client.Commands;

public class JoinCommand(IDataSender sender, PacketConsumer<JoinResponsePacket> joinResponseConsumer) : Command
{
    public override string Option => "join";
    public override string Description => "Join the server with a charcter.";
    public override string ArgumentsHint => "<character UID>";

    private readonly IDataSender _sender = sender;
    private readonly PacketConsumer<JoinResponsePacket> _joinResponseConsumer = joinResponseConsumer;

    protected override async Task<CommandState> InvokeAsync(ReadOnlyQueue<string> args)
    {
        string characterUid = args.Take();

        if (string.IsNullOrWhiteSpace(characterUid))
        {
            return CommandState.Failure;
        }

        var packet = new Packet(PacketType.JoinRequest, 1, new JoinRequestPacket(characterUid).Serialize());
        var joinResponseAwaiter = _joinResponseConsumer.GetPacketAwaiter();

        _sender.Send(packet.Serialize(), new All<Session>());
        JoinResponsePacket joinResponse = await joinResponseAwaiter.WaitAsync();

        return joinResponse.Success ? CommandState.Success : CommandState.Failure;
    }
}