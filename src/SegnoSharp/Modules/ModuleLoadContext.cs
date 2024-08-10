using System.Reflection;
using System.Runtime.Loader;
using System;
using System.Linq;

namespace Whitestone.SegnoSharp.Modules
{
    internal class ModuleLoadContext(string modulePath) : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver = new(modulePath);

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Fallback to loading the assembly from default context if it exists there
            if (Default.Assemblies.Any(a => a.FullName == assemblyName.FullName))
                return null;

            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
        }
    }
}
