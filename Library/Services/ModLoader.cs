using System.Reflection;
using Library.Configuration;
using Library.Configuration.Modding;
using Library.IO;
using Library.Util;
using Microsoft.Extensions.Logging;
using Swordfish.Library.IO;
using Path = Swordfish.Library.IO.Path;

namespace Library.Services;

public class ModLoader : IModLoader
{
    private readonly ILogger _logger;
    private readonly IFileService _fileService;
    private readonly ModOptions? _options;
    private readonly IReadOnlyCollection<ParsedFile<ModManifest>> _manfiests;

    public ModLoader(ILogger logger, IFileService fileService, ConfigurationProvider configurationProvider)
    {
        _logger = logger;
        _fileService = fileService;
        _options = configurationProvider.GetModOptions()?.Value;
        _manfiests = configurationProvider.GetModManifests();
    }

    public void Load()
    {
        if (_manfiests.Count == 0)
        {
            _logger.LogInformation("No mods found to load.");
            return;
        }

        if (_options == null)
        {
            _logger.LogError("No mod options were found, but there are {count} mods present. Mods will not be loaded.", _manfiests.Count);
            return;
        }

        if (_options.LoadOrder == null || _options.LoadOrder.Length == 0)
        {
            _logger.LogError("Mod load order is not specified, but there are {count} mods present. Mods will not be loaded.", _manfiests.Count);
            return;
        }

        if (_options.LoadOrder.Length != _manfiests.Count || !_manfiests.All(manfiest => _options.LoadOrder.Contains(manfiest.Value.ID)))
        {
            _logger.LogWarning("Not all present mods are specified in the load order. Some mods will not be loaded.");
        }

        if (!_options.LoadOrder.All(id => _manfiests.Select(manfiest => manfiest.Value.ID).Contains(id)))
        {
            _logger.LogWarning("Some mods specified in the load order are missing and will not be loaded.");
        }

        if (_options.AllowScriptCompilation)
        {
            _logger.LogInformation("Script compilation is enabled.");
        }
        else
        {
            _logger.LogInformation("Script compilation is disabled.");
        }

        foreach (string id in _options.LoadOrder)
        {
            ParsedFile<ModManifest>? manifestFile = _manfiests.FirstOrDefault(manfiest => manfiest.Value.ID == id);
            if (manifestFile == null)
            {
                _logger.LogError("Tried to load mod ID \"{id}\" from the load order but it could not be found. Is it missing a manifest?", id);
                continue;
            }

            ModManifest manifest = manifestFile.Value;
            IPath modDirectory = manifestFile.Path.GetDirectory();

            Result<Exception?> result = LoadMod(_logger, _fileService, _options, manifest, modDirectory);
            if (result)
            {
                _logger.LogInformation("Loaded mod \"{name}\" ({id}), by \"{author}\": {description}", manifest.Name, manifest.ID, manifest.Author, manifest.Description);
            }
            else
            {
                _logger.LogError(result.Value, "Failed to load mod \"{name}\" ({id}), by \"{author}\". {message}.", manifest.Name, manifest.ID, manifest.Author, result.Message);
            }
        }
    }

    private static Result<Exception?> LoadMod(ILogger logger, IFileService fileService, ModOptions options, ModManifest manifest, IPath directory)
    {
        IPath rootPath = manifest.RootPathOverride ?? directory;

        //  Compile any scripts into an assembly
        IPath scriptsPath = manifest.ScriptsPath ?? new Path("Scripts/");
        scriptsPath = rootPath.At(scriptsPath);
        if (scriptsPath.Exists())
        {
            var scriptFiles = fileService.GetFiles(scriptsPath, SearchOption.AllDirectories);
            if (options.AllowScriptCompilation && scriptFiles.Length > 0)
            {
                //  TODO implement script compilation
                logger.LogError("Unable to compile scripts in mod {name} ({id}), script compilation is not supported.", manifest.Name, manifest.ID);
            }
        }

        //  Load any external assemblies
        if (manifest.Assemblies != null && manifest.Assemblies.Length > 0)
        {
            IPath assembliesPath = manifest.AssembliesPath ?? new Path("");

            var loadedAssemblies = new List<Assembly>();
            foreach (string assemblyName in manifest.Assemblies)
            {
                IPath assemblyPath = rootPath.At(assembliesPath).At(assemblyName);
                try
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyPath.ToString());
                    loadedAssemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    return new Result<Exception?>(false, ex, $"Error loading assembly: {assemblyName}");
                }
            }

            foreach (Assembly assembly in loadedAssemblies)
            {
                try
                {
                    HookAssembly(assembly);
                }
                catch (Exception ex)
                {
                    return new Result<Exception?>(false, ex, $"Error hooking into assembly: {assembly.FullName}");
                }
            }
        }

        return new Result<Exception?>(true, null);
    }

    private static void HookAssembly(Assembly assembly)
    {
        //  TODO bind container, attach events, etc.
    }
}
