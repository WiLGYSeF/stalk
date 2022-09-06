using System.Reflection;

namespace Wilgysef.Stalk.Application.AssemblyLoaders;

internal static class AssemblyLoader
{
    public static List<Assembly> LoadAssemblies(string path)
    {
        var assemblies = new List<Assembly>();
        foreach (var file in Directory.GetFiles(path))
        {
            try
            {
                assemblies.Add(Assembly.LoadFile(file));
            }
            catch (BadImageFormatException) { }
        }
        return assemblies;
    }
}
