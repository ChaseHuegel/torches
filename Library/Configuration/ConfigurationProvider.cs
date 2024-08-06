using Library.Configuration.Localization;
using Library.Configuration.Modding;
using Library.IO;
using Microsoft.Extensions.Logging;
using Swordfish.Library.IO;

namespace Library.Configuration;

public class ConfigurationProvider
{
    private const string FILE_MOD_OPTIONS = "ModOptions.toml";

    private readonly ParsedFile<Language>[] _languages;
    private readonly ParsedFile<ModManifest>[] _modManifests;
    private readonly ParsedFile<ModOptions>? _modOptions;

    public ConfigurationProvider(ILogger logger, IFileService fileService)
    {
        _languages = LoadLanguageDefinitions(fileService);
        logger.LogInformation("Found {count} languages.", _languages.Length);

        _modManifests = LoadModManifests(fileService);
        logger.LogInformation("Found {count} mod manifiests.", _modManifests.Length);

        IPath modOptionsPath = Paths.Config.At(FILE_MOD_OPTIONS);
        if (fileService.TryParse(modOptionsPath, out ModOptions modOptions))
        {
            _modOptions = new ParsedFile<ModOptions>(modOptionsPath, modOptions);
            logger.LogInformation("Found mod options.");
        }
        else
        {
            logger.LogInformation("No mod options found.");
        }
    }

    public IReadOnlyCollection<ParsedFile<Language>> GetLanguages()
    {
        return _languages;
    }

    public ParsedFile<ModOptions>? GetModOptions()
    {
        return _modOptions;
    }

    public IReadOnlyCollection<ParsedFile<ModManifest>> GetModManifests()
    {
        return _modManifests;
    }

    private static ParsedFile<Language>[] LoadLanguageDefinitions(IFileService fileService)
    {
        IPath[] langFiles = fileService.GetFiles(Paths.Lang, "*.toml", SearchOption.AllDirectories);

        var languages = new List<ParsedFile<Language>>();
        for (int i = 0; i < langFiles.Length; i++)
        {
            IPath path = langFiles[i];
            if (fileService.TryParse(path, out Language language))
            {
                languages.Add(new ParsedFile<Language>(path, language));
            }
        }

        return [.. languages];
    }

    private static ParsedFile<ModManifest>[] LoadModManifests(IFileService fileService)
    {
        IPath[] modFiles = fileService.GetFiles(Paths.Mods, "Manifest.toml", SearchOption.AllDirectories);

        var manifests = new List<ParsedFile<ModManifest>>();
        for (int i = 0; i < modFiles.Length; i++)
        {
            IPath path = modFiles[i];
            if (fileService.TryParse(path, out ModManifest manifest))
            {
                manifests.Add(new ParsedFile<ModManifest>(path, manifest));
            }
        }

        return [.. manifests];
    }
}