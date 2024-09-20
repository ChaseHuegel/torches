using Library.Collections;
using Library.Types;
using Networking.LowLevel;
using Packets;
using Packets.Auth;
using Swordfish.Library.Collections;
using Swordfish.Library.IO;

namespace Chat.Client.Commands;

public class LogoutCommand(IDataSender sender) : Command
{
    public override string Option => "logout";
    public override string Description => "Logout from the server.";
    public override string ArgumentsHint => "";

    private readonly IDataSender _sender = sender;

    protected override Task<CommandState> InvokeAsync(ReadOnlyQueue<string> args)
    {
        var packet = new Packet(PacketType.Logout, new LogoutPacket(1).Serialize());
        _sender.Send(packet.Serialize(), new All<Session>());

        return Task.FromResult(CommandState.Success);
    }
}