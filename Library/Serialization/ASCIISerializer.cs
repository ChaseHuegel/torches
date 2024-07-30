using System.Text;

namespace Library.Serialization;

public class ASCIISerializer : ISerializer<string>
{
    public byte[] Serialize(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    public string Deserialize(byte[] data)
    {
        return Encoding.ASCII.GetString(data);
    }
}