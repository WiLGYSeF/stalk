using System.Diagnostics;
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
                assemblies.Add(Assembly.LoadFrom(file));
            }
            catch (BadImageFormatException) { }
            catch (FileLoadException)
            {
                Console.WriteLine($"Failed to load external assembly: {file}");
                Debug.WriteLine($"Failed to load external assembly: {file}");
                throw;
            }
        }
        return assemblies;
    }
}
