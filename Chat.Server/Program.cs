using Chat.Server;
using Library.Configuration;
using SmartFormat.Core.Extensions;
using SmartFormat.Utilities;

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

        Container.Register<ConfigurationProvider>(Reuse.Singleton);

        Container.Register<ILocalizationProvider, Library.Configuration.Localization.LocalizationProvider>(Reuse.Singleton);
        Container.Register<IFormatter, LocalizationFormatter>();
        Container.RegisterDelegate(static () => Container.Resolve<ConfigurationProvider>().GetLanguages());

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