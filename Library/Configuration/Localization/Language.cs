namespace Library.Configuration.Localization;

public class Language(string twoLetterISOLanguageName) : TomlConfiguration<Language>
{
    public readonly Dictionary<string, string> Translations = [];

    [TomlProperty("Language")]
    public string TwoLetterISOLanguageName { get; private set; } = twoLetterISOLanguageName;

    public string Translate(string value, params object[] args)
    {
        var str = Smart.Format(value, args);
        return str;
    }
}