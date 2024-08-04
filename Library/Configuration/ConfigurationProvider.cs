using Library.Configuration.Localization;
using Library.Configuration.Modding;
using Library.IO;
using Microsoft.Extensions.Logging;
using Swordfish.Library.IO;

namespace Library.Configuration;

public class ConfigurationProvider
{
    private const string FILE_MOD_OPTIONS = "ModOptions.toml";

    private readonly Language[] _languages;
    private readonly ModManifest[] _modManifests;
    private readonly ModOptions? _modOptions;

    public ConfigurationProvider(ILogger logger, IFileService fileService)
    {
        _languages = LoadLanguageDefinitions(fileService);
        logger.LogInformation("Found {count} languages.", _languages.Length);

        _modManifests = LoadModManifests(fileService);
        logger.LogInformation("Found {count} mod manifiests.", _modManifests.Length);

        bool loadedModOptions = fileService.TryParse(Paths.Config.At(FILE_MOD_OPTIONS), out _modOptions);
        if (loadedModOptions)
        {
            logger.LogInformation("Found mod options.");
        }
        else
        {
            logger.LogInformation("No mod options found.");
        }
    }

    public IReadOnlyCollection<Language> GetLanguages()
    {
        return _languages;
    }

    public ModOptions? GetModOptions()
    {
        return _modOptions;
    }

    public IReadOnlyCollection<ModManifest> GetModManifests()
    {
        return _modManifests;
    }

    private static Language[] LoadLanguageDefinitions(IFileService fileService)
    {
        IPath[] langFiles = fileService.GetFiles(Paths.Lang, "*.toml", SearchOption.AllDirectories);

        var languageDefinitions = new List<Language>();
        for (int i = 0; i < langFiles.Length; i++)
        {
            if (fileService.TryParse(langFiles[i], out Language language))
            {
                languageDefinitions.Add(language);
            }
        }

        return [.. languageDefinitions];
    }

    private static ModManifest[] LoadModManifests(IFileService fileService)
    {
        IPath[] modFiles = fileService.GetFiles(Paths.Mods, "Manifest.toml", SearchOption.AllDirectories);

        var manifests = new List<ModManifest>();
        for (int i = 0; i < modFiles.Length; i++)
        {
            if (fileService.TryParse(modFiles[i], out ModManifest manifest))
            {
                manifests.Add(manifest);
            }
        }

        return [.. manifests];
    }
}