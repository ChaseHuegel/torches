
using Library.Configuration.Localization;

namespace Library.Configuration;

public class ConfigurationProvider
{
    private const string ConfigDirectory = "Assets/Config";

    private Language[]? _languages;

    public ConfigurationProvider()
    {
        LoadLangFiles();
    }

    public IReadOnlyCollection<Language> GetLanguages()
    {
        return _languages ?? [];
    }

    private void LoadLangFiles()
    {
        string[] langFiles = Directory.GetFiles($"{ConfigDirectory}/Lang", "*.toml", SearchOption.AllDirectories)
            .Where(file => Path.GetExtension(file).Equals(".toml"))
            .ToArray();

        var languageDefinitions = new Language[langFiles.Length];
        for (int i = 0; i < langFiles.Length; i++)
        {
            string? langFile = langFiles[i];
            var content = File.ReadAllText(langFile);
            languageDefinitions[i] = Language.FromString(content);
        }

        _languages = languageDefinitions;
    }
}