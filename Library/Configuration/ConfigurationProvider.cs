using Library.Configuration.Localization;
using Library.Configuration.Modding;
using Library.IO;
using Microsoft.Extensions.Logging;
using Swordfish.Library.IO;

namespace Library.Configuration;

public class ConfigurationProvider
{
    private const string FILE_MODULE_OPTIONS = "Modules.toml";

    private readonly ParsedFile<Language>[] _languages;
    private readonly ParsedFile<ModuleManfiest>[] _modManifests;
    private readonly ParsedFile<ModuleOptions>? _modOptions;

    public ConfigurationProvider(ILogger logger, IFileService fileService)
    {
        _languages = LoadLanguageDefinitions(fileService);
        logger.LogInformation("Found {count} languages.", _languages.Length);

        _modManifests = LoadModManifests(fileService);
        logger.LogInformation("Found {count} module manifiests.", _modManifests.Length);

        IPath modOptionsPath = Paths.Config.At(FILE_MODULE_OPTIONS);
        if (fileService.TryParse(modOptionsPath, out ModuleOptions modOptions))
        {
            _modOptions = new ParsedFile<ModuleOptions>(modOptionsPath, modOptions);
            logger.LogInformation("Found module options.");
        }
        else
        {
            logger.LogInformation("No module options found.");
        }
    }

    public IReadOnlyCollection<ParsedFile<Language>> GetLanguages()
    {
        return _languages;
    }

    public ParsedFile<ModuleOptions>? GetModOptions()
    {
        return _modOptions;
    }

    public IReadOnlyCollection<ParsedFile<ModuleManfiest>> GetModuleManifests()
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

    private static ParsedFile<ModuleManfiest>[] LoadModManifests(IFileService fileService)
    {
        IPath[] modFiles = fileService.GetFiles(Paths.Modules, "Manifest.toml", SearchOption.AllDirectories);

        var manifests = new List<ParsedFile<ModuleManfiest>>();
        for (int i = 0; i < modFiles.Length; i++)
        {
            IPath path = modFiles[i];
            if (fileService.TryParse(path, out ModuleManfiest manifest))
            {
                manifests.Add(new ParsedFile<ModuleManfiest>(path, manifest));
            }
        }

        return [.. manifests];
    }
}