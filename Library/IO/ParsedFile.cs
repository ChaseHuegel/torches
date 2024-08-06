using Swordfish.Library.IO;

namespace Library.IO;

public class ParsedFile<T>(IPath path, T value)
{
    public readonly IPath Path = path;
    public readonly T Value = value;
}