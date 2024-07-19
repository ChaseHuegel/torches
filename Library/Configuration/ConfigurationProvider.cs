
using Library.Configuration.Localization;
using Library.IO;
using Swordfish.Library.IO;

namespace Library.Configuration;

public class ConfigurationProvider
{
    private readonly IFileService _fileService;
    private Language[]? _languages;

    public ConfigurationProvider(IFileService fileService)
    {
        _fileService = fileService;
        LoadLanguageDefinitions();
    }

    public IReadOnlyCollection<Language> GetLanguages()
    {
        return _languages ?? [];
    }

    private void LoadLanguageDefinitions()
    {
        IPath[] langFiles = _fileService.GetFiles(Paths.Lang, SearchOption.AllDirectories);

        var languageDefinitions = new Language[langFiles.Length];
        for (int i = 0; i < langFiles.Length; i++)
        {
            if (_fileService.TryParse(langFiles[i], out Language language))
            {
                languageDefinitions[i] = language;
            }
        }

        _languages = languageDefinitions;
    }
}