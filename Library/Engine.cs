using System.Reflection;
using Library.Configuration;
using Library.Configuration.Localization;
using Library.Configuration.Modding;
using Library.DependencyInjection;
using Library.Events;
using Library.Serialization;
using Library.Serialization.Toml;
using Library.Services;
using Library.Util;
using Microsoft.Extensions.Logging;
using SmartFormat.Core.Extensions;
using Swordfish.Library.IO;

namespace Library;

public sealed class Engine : IDisposable
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger _logger;

    public IContainer? Container => _container;

    private IContainer? _container;

    static Engine()
    {
        _logger = CreateLogger<Engine>();
    }

    public void Start(string[] args)
    {
        IContainer coreContainer = CreateCoreContainer();
        ActivateTomlMappers(coreContainer);

        IContainer modulesContainer = CreateModulesContainer(coreContainer);
        ActivateTomlMappers(modulesContainer);

        _container = modulesContainer;
    }

    public void Dispose()
    {
        _container?.Dispose();
    }

    private static IContainer CreateCoreContainer()
    {
        IContainer container = new Container();

        container.RegisterInstance<TextWriter>(Console.Out);

        container.Register<IModulesLoader, ModulesLoader>(Reuse.Singleton);

        container.Register<ILogger>(Made.Of(() => CreateLogger(Arg.Index<Request>(0)), request => request));

        container.Register<ConfigurationProvider>(Reuse.Singleton);

        container.Register<IFileService, FileService>(Reuse.Singleton);
        container.Register<IFileParser, TomlParser<Language>>(Reuse.Singleton);
        container.Register<IFileParser, TomlParser<ModuleOptions>>(Reuse.Singleton);
        container.Register<IFileParser, TomlParser<ModuleManfiest>>(Reuse.Singleton);

        container.Register<ITomlMapper, PathTomlMapper>(Reuse.Singleton);

        container.RegisterDelegate<SmartFormatter>(SmartFormatterProvider.Resolve);
        container.Register<ILocalizationProvider, LocalizationService>(Reuse.Singleton);
        container.Register<IFormatter, LocalizationFormatter>(Reuse.Singleton);
        container.RegisterDelegate<IReadOnlyCollection<Language>>(() => container.Resolve<ConfigurationProvider>().GetLanguages().Select(languageFile => languageFile.Value).ToList());

        ValidateContainerOrDie(container);
        return container;
    }

    private static IContainer CreateModulesContainer(IContainer parentContainer)
    {
        IContainer container = parentContainer.With();
        parentContainer.Resolve<IModulesLoader>().Load(HookCallback);

        void HookCallback(Assembly assembly)
        {
            RegisterEventProcessors(assembly, container);
            RegisterSerializers(assembly, container);
            RegisterCommands(assembly, container);
            RegisterDryIocModules(assembly, container);
        }

        container.Register<CommandParser>(Reuse.Singleton, made: Made.Of(() => new CommandParser(Arg.Index<char>(0), Arg.Of<Command[]>()), _ => '\0'));

        ValidateContainerOrDie(container);
        return container;
    }

    private static void ValidateContainerOrDie(IContainer container)
    {
        KeyValuePair<ServiceInfo, ContainerException>[] errors = container.Validate();
        if (errors.Length > 0)
        {
            foreach (KeyValuePair<ServiceInfo, ContainerException> error in errors)
            {
                _logger.LogError(error.Value, "There was an error validating a container (service: {service}).", error.Key);
            }
            Environment.Exit(1);
        }
    }

    private static void RegisterSerializers(Assembly assembly, IContainer container)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract)
            {
                continue;
            }

            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length == 0)
            {
                continue;
            }

            foreach (Type interfaceType in interfaces)
            {
                if (!interfaceType.IsGenericType)
                {
                    continue;
                }

                Type genericTypeDef = interfaceType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(ISerializer<>))
                {
                    container.RegisterMany(serviceTypes: interfaces, implType: type, reuse: Reuse.Singleton);
                    break;
                }
            }
        }
    }

    private static void RegisterEventProcessors(Assembly assembly, IContainer container)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract)
            {
                continue;
            }

            if (!type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventProcessor<>)))
            {
                continue;
            }

            container.RegisterMany(serviceTypes: type.GetInterfaces(), implType: type, reuse: Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.AppendNewImplementation);
        }
    }

    private static void RegisterCommands(Assembly assembly, IContainer container)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract)
            {
                continue;
            }

            if (!type.IsAssignableTo<Command>())
            {
                continue;
            }

            container.Register(typeof(Command), type, reuse: Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.AppendNewImplementation);
            _logger.LogInformation("Registered command of type: {type}.", type);
        }
    }

    private static void RegisterDryIocModules(Assembly assembly, IContainer container)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract)
            {
                continue;
            }

            if (!typeof(IDryIocModule).IsAssignableFrom(type))
            {
                continue;
            }

            var containerModule = (IDryIocModule)Activator.CreateInstance(type)!;
            containerModule.Load(container);
        }
    }

    private static void ActivateTomlMappers(IContainer container)
    {
        foreach (ITomlMapper mapper in container.ResolveMany<ITomlMapper>())
        {
            mapper.Register();
        }
    }

    private static ILogger CreateLogger(Request request)
    {
        return _loggerFactory.CreateLogger(request.Parent.ImplementationType);
    }

    public static ILogger CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }

    public static ILogger CreateLogger(Type type)
    {
        return _loggerFactory.CreateLogger(type);
    }
}