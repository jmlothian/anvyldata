using Microsoft.Extensions.DependencyModel;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Anvyl.Reflection.Extensions
{
    //https://stackoverflow.com/questions/40908568/assembly-loading-in-net-core
    public static class AssemblyLoader
    {
        private static bool IsCandidateLibrary(RuntimeLibrary library, AssemblyName assemblyName)
        {
            return (library.Name == (assemblyName.Name))
                    || (library.Dependencies.Any(d => d.Name.StartsWith(assemblyName.Name)));
        }
        static AssemblyLoader()
        {
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                // avoid loading *.resources dlls, because of: https://github.com/dotnet/coreclr/issues/8416
                if (name.Name.EndsWith("resources"))
                {
                    return null;
                }

                var dependencies = DependencyContext.Default.RuntimeLibraries;
                foreach (var library in dependencies)
                {
                    if (IsCandidateLibrary(library, name))
                    {
                        return context.LoadFromAssemblyName(new AssemblyName(library.Name));
                    }
                }

                var foundDlls = Directory.GetFileSystemEntries(new FileInfo(< YOUR_PATH_HERE >).FullName, name.Name + ".dll", SearchOption.AllDirectories);
                if (foundDlls.Any())
                {
                    return context.LoadFromAssemblyPath(foundDlls[0]);
                }

                return context.LoadFromAssemblyName(name);
            };
        }
        public static Assembly Load(string assemblyFullPath)
        {
            var fileNameWithOutExtension = Path.GetFileNameWithoutExtension(assemblyFullPath);

            var inCompileLibraries = DependencyContext.Default.CompileLibraries.Any(l => l.Name.Equals(fileNameWithOutExtension, StringComparison.OrdinalIgnoreCase));
            var inRuntimeLibraries = DependencyContext.Default.RuntimeLibraries.Any(l => l.Name.Equals(fileNameWithOutExtension, StringComparison.OrdinalIgnoreCase));

            var assembly = (inCompileLibraries || inRuntimeLibraries)
                ? Assembly.Load(new AssemblyName(fileNameWithOutExtension))
                : AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFullPath);

            return assembly;
        }
    }
}
