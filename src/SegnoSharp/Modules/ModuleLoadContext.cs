using System.Reflection;
using System.Runtime.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Whitestone.SegnoSharp.Modules
{
    internal class ModuleLoadContext(string modulePath, List<string> additionalAssemblyFolders) : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver = new(modulePath);

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Fallback to loading the assembly from default context if it exists there
            if (Default.Assemblies.Any(a => a.FullName == assemblyName.FullName))
                return null;

            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            if (additionalAssemblyFolders == null)
            {
                return null;
            }

            foreach (string assemblyFolder in additionalAssemblyFolders)
            {
                string potentialAssemblyPath = Path.Combine(assemblyFolder, $"{assemblyName.Name}.dll");
                FileInfo fi = new(potentialAssemblyPath);
                if (fi.Exists)
                {
                    return LoadFromAssemblyPath(fi.FullName);
                }
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

            if (!string.IsNullOrEmpty(libraryPath))
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dll" : "so";

            foreach (string assemblyFolder in additionalAssemblyFolders)
            {
                string potentialAssemblyPath = Path.Combine(assemblyFolder, $"{unmanagedDllName}.{extension}");
                FileInfo fi = new(potentialAssemblyPath);
                if (fi.Exists)
                {
                    return LoadUnmanagedDllFromPath(fi.FullName);
                }
            }

            return IntPtr.Zero;
        }
    }
}
