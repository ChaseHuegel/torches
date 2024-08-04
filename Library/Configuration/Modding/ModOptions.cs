namespace Library.Configuration.Modding;

public class ModOptions(bool allowScriptCompilation, string[] loadOrder) : TomlConfiguration<ModOptions>
{
    [TomlProperty("AllowScriptCompilation")]
    public bool AllowScriptCompilation { get; private set; } = allowScriptCompilation;

    [TomlProperty("LoadOrder")]
    public string[]? LoadOrder { get; private set; } = loadOrder;
}