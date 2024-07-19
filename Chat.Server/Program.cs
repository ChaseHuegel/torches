using Chat.Server;
using Library.Configuration;
using Library.Configuration.Localization;
using Library.IO;
using Networking;
using SmartFormat.Core.Extensions;
using SmartFormat.Utilities;
using Swordfish.Library.IO;

internal static class Program
{
    private static readonly Container Container = new();

    private static void Main(string[] args)
    {
        SetupContainer();
        SetupLocalization();

        var application = Container.Resolve<Application>();
        application.Run();
    }

    private static void SetupContainer()
    {
        Container.Register<Application>(Reuse.Singleton);

        Container.Register<IFileService, FileService>(Reuse.Singleton);
        Container.Register<IFileParser, LanguageParser>(Reuse.Singleton);
        Container.Register<ConfigurationProvider>(Reuse.Singleton);

        Container.Register<ILocalizationProvider, LocalizationService>(Reuse.Singleton);
        Container.Register<IFormatter, LocalizationFormatter>();
        Container.RegisterDelegate<IReadOnlyCollection<Language>>(static () => Container.Resolve<ConfigurationProvider>().GetLanguages());

        Container.Register<SessionService>(Reuse.Singleton);

        Container.ValidateAndThrow();
    }

    private static void SetupLocalization()
    {
        Smart.Default.Settings.Localization.LocalizationProvider = Container.Resolve<ILocalizationProvider>();

        foreach (var formatter in Container.ResolveMany<IFormatter>())
        {
            Smart.Default.AddExtensions(formatter);
        }

        foreach (var source in Container.ResolveMany<ISource>())
        {
            Smart.Default.AddExtensions(source);
        }
    }
}