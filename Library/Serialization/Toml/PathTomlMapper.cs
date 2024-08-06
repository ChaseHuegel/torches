using Swordfish.Library.IO;
using Tomlet;
using Tomlet.Exceptions;
using Tomlet.Models;
using Path = Swordfish.Library.IO.Path;

namespace Library.Serialization.Toml;

public class PathTomlMapper : ITomlMapper
{
    public void Register()
    {
        TomletMain.RegisterMapper(Serialize, Deserialize);
        TomletMain.RegisterMapper(SerializeGeneric, DeserializeGeneric);
    }

    private TomlValue? Serialize(Path path)
    {
        return SerializeGeneric(path);
    }

    private Path Deserialize(TomlValue value)
    {
        return (Path)DeserializeGeneric(value);
    }

    private TomlValue? SerializeGeneric(IPath? path)
    {
        return path is null ? null : new TomlString(path.ToString());
    }

    private IPath DeserializeGeneric(TomlValue value)
    {
        if (value is not TomlString tomlString)
        {
            throw new TomlTypeMismatchException(typeof(TomlString), value.GetType(), typeof(Path));
        }

        return new Path(tomlString.Value);
    }
}