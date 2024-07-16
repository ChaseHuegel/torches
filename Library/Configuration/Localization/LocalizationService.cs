namespace Library.Configuration.Localization;

public class LocalizationService : ILocalizationProvider
{
    private readonly Dictionary<string, Language> _languages = [];

    public LocalizationService(IReadOnlyCollection<Language> languageDefinitions)
    {
        foreach (Language languageDefinition in languageDefinitions)
        {
            string key = languageDefinition.TwoLetterISOLanguageName;
            if (_languages.TryGetValue(key, out Language? language))
            {
                foreach (var translation in languageDefinition.Translations)
                {
                    language.Translations.Add(translation.Key, translation.Value);
                }
            }
            else
            {
                _languages.Add(key, languageDefinition);
            }
        }
    }

    public string? GetString(string name)
    {
        return GetTranslation(name, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    public string? GetString(string name, string cultureName)
    {
        return GetTranslation(name, cultureName);
    }

    public string? GetString(string name, CultureInfo cultureInfo)
    {
        return GetTranslation(name, cultureInfo.TwoLetterISOLanguageName);
    }

    private string? GetTranslation(string name, string cultureName)
    {
        if (!_languages.TryGetValue(cultureName, out Language? language))
        {
            return null;
        }

        return language.Translations.TryGetValue(name, out string? translation) ? translation : null;
    }
}