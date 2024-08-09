using Library.Collections;
using Library.Types;
using Networking.Messaging;
using Packets.Chat;
using Swordfish.Library.Collections;
using Swordfish.Library.IO;

namespace Networking.Commands;

public class ExitCommand : Command
{
    public override string Option => "exit";
    public override string Description => "Immediately exit the application.";
    public override string ArgumentsHint => "";

    protected override Task<CommandState> InvokeAsync(ReadOnlyQueue<string> args)
    {
        Environment.Exit(0);
        return Task.FromResult(CommandState.Success);
    }
}