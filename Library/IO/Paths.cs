namespace Library.IO;

using Swordfish.Library.IO;

public static class Paths
{
    private static IPath? _assets;
    private static IPath? _lang;
    private static IPath? _config;
    private static IPath? _modules;

    public static IPath Assets => _assets ??= new Path("Assets/");
    public static IPath Config => _config ??= Assets.At("Config/");
    public static IPath Lang => _lang ??= Assets.At("Lang/");
    public static IPath Modules => _modules ??= Assets.At("Modules/");
}