namespace Library.Serialization;

public interface IParser
{
    List<byte[]> Parse(byte[] data);
}
