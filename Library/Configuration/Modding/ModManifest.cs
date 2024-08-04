using Swordfish.Library.IO;

namespace Library.Configuration.Modding;

public class ModManifest(string id, string name) : TomlConfiguration<ModManifest>
{
    [TomlProperty("ID")]
    public string? ID { get; private set; } = id;

    [TomlProperty("Name")]
    public string? Name { get; private set; } = name;

    [TomlProperty("Description")]
    public string? Description { get; init; }

    [TomlProperty("Author")]
    public string? Author { get; init; }

    [TomlProperty("Website")]
    public string? Website { get; init; }

    [TomlProperty("Source")]
    public string? Source { get; init; }

    [TomlProperty("RootPathOverride")]
    public IPath? RootPathOverride { get; init; }

    [TomlProperty("ScriptsPath")]
    public IPath? ScriptsPath { get; init; }

    [TomlProperty("AssembliesPath")]
    public IPath? AssembliesPath { get; init; }

    [TomlProperty("Assemblies")]
    public string[]? Assemblies { get; init; }
}