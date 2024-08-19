using System.Reflection;

namespace Library.Services;

public interface IModulesLoader
{
    void Load(Action<Assembly> hookCallback);
}