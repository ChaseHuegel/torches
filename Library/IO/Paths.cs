namespace Library.IO;

using Swordfish.Library.IO;

public static class Paths
{
    private static IPath? _assets;
    private static IPath? _lang;

    public static IPath Assets => _assets ??= new Path("Assets");
    public static IPath Lang => _lang ??= Assets.At("Lang");
}