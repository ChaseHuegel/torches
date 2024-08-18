using Library.Events;
using Library.Util;
using Networking.Events;
using Packets.Chat;

namespace Chat.Client.Processors;

public class TextPacketProcessor(TextWriter textWriter) : IEventProcessor<MessageEventArgs<TextPacket>>
{
    private readonly TextWriter _textWriter = textWriter;

    public Result<EventBehavior> ProcessEvent(object? sender, MessageEventArgs<TextPacket> e)
    {
        TextPacket text = e.Message;
        _textWriter.WriteLine(text.Value);
        return new Result<EventBehavior>(true, EventBehavior.Continue);
    }
}