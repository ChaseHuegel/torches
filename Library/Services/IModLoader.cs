using System.Reflection;
using Library.Util;

namespace Library.Services;

public interface IModLoader
{
    void Load(Action<Assembly> hookCallback);
}