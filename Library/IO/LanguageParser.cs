namespace Library.IO;

using Library.Configuration.Localization;
using Swordfish.Library.IO;

public class LanguageParser : IFileParser<Language>
{
    public string[] SupportedExtensions { get; } = [
        ".toml"
    ];

    object IFileParser.Parse(IFileService fileService, IPath file) => Parse(fileService, file);
    public Language Parse(IFileService fileService, IPath file)
    {
        return Language.FromString(fileService.ReadString(file));
    }
}