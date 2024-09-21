using Library.Collections;
using Library.Types;
using Networking.LowLevel;
using Networking.Messaging;
using Packets;
using Packets.Auth;
using Swordfish.Library.Collections;
using Swordfish.Library.IO;

namespace Chat.Client.Commands;

public class LoginCommand(IDataSender sender, PacketConsumer<LoginResponsePacket> loginResponseConsumer) : Command
{
    public override string Option => "login";
    public override string Description => "Login to the server.";
    public override string ArgumentsHint => "<token>";

    private readonly IDataSender _sender = sender;
    private readonly PacketConsumer<LoginResponsePacket> _loginResponseConsumer = loginResponseConsumer;

    protected override async Task<CommandState> InvokeAsync(ReadOnlyQueue<string> args)
    {
        string token = args.Take();

        if (string.IsNullOrWhiteSpace(token))
        {
            return CommandState.Failure;
        }

        var packet = new Packet(PacketType.LoginRequest, 1, new LoginRequestPacket(token).Serialize());
        var loginResponseAwaiter = _loginResponseConsumer.GetPacketAwaiter();

        _sender.Send(packet.Serialize(), new All<Session>());
        LoginResponsePacket loginResponse = await loginResponseAwaiter.WaitAsync();

        return loginResponse.Success ? CommandState.Success : CommandState.Failure;
    }
}