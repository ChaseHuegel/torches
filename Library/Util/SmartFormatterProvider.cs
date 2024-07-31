using SmartFormat.Core.Extensions;
using SmartFormat.Core.Settings;

namespace Library.Util;

public static class SmartFormatterProvider
{
    [ThreadStatic]
    private static SmartFormatter? SmartFormatter;

    public static SmartFormatter Resolve(IResolverContext context)
    {
        if (SmartFormatter != null)
        {
            return SmartFormatter;
        }

        SmartSettings settings = new()
        {
            Localization = {
                LocalizationProvider = context.Resolve<ILocalizationProvider>()
            }
        };

        SmartFormatter = Smart.CreateDefaultSmartFormat(settings);

        foreach (var formatter in context.ResolveMany<IFormatter>())
        {
            SmartFormatter.AddExtensions(formatter);
        }

        foreach (var source in context.ResolveMany<ISource>())
        {
            SmartFormatter.AddExtensions(source);
        }

        return SmartFormatter;
    }
}