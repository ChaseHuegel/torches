namespace Library.Configuration.Modding;

public class ModuleOptions(bool allowScriptCompilation, string[] loadOrder) : TomlConfiguration<ModuleOptions>
{
    [TomlProperty("AllowScriptCompilation")]
    public bool AllowScriptCompilation { get; private set; } = allowScriptCompilation;

    [TomlProperty("LoadOrder")]
    public string[]? LoadOrder { get; private set; } = loadOrder;
}