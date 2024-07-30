namespace Library.Serialization;

public class DirectParser : IParser
{
    public List<byte[]> Parse(byte[] data)
    {
        return [data];
    }
}