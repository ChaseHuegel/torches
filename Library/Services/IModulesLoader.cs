using System.Reflection;
using Library.Util;

namespace Library.Services;

public interface IModulesLoader
{
    void Load(Action<Assembly> hookCallback);
}