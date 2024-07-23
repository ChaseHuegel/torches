namespace Library.Types;

public struct Session
{
    public uint ID;

    internal Session(uint id)
    {
        ID = id;
    }

    public override readonly string ToString()
    {
        return ID.ToString();
    }
}