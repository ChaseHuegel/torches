using Library.Types;

namespace Chat.Server.Types;

public readonly struct ChatMessage(int channel, Session sender, Session? target, string value)
{
    public readonly int Channel = channel;
    public readonly Session Sender = sender;
    public readonly Session? Target = target;
    public readonly string Value = value;
}