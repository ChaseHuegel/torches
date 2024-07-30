using Library.Types;

namespace Networking.Events;

public readonly struct DataEventArgs(byte[] bytes, Session sender)
{
    public readonly byte[] Data = bytes;

    public readonly Session Sender = sender;
}