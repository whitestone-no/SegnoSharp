using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Whitestone.SegnoSharp.Common.Interfaces;
using Microsoft.AspNetCore.Builder;

namespace Whitestone.SegnoSharp.Modules
{
    internal static class ServiceCollectionExtensions
    {
        internal static IEnumerable<IModule> AddModules(this WebApplicationBuilder builder)
        {
            var modulesConfiguration = builder.Configuration.GetSection(Configuration.Models.Modules.Section).Get<Configuration.Models.Modules>();
            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var modulesFolder = new DirectoryInfo(Path.Combine(executingFile.DirectoryName!, modulesConfiguration.ModulesFolder));

            if (!modulesFolder.Exists)
            {
                yield break;
            }

            foreach (DirectoryInfo moduleFolder in modulesFolder.EnumerateDirectories())
            {
                FileInfo moduleFile = moduleFolder.GetFiles("*.dll").FirstOrDefault(f => f.Name.StartsWith("Module."));

                if (moduleFile == null)
                {
                    continue;
                }

                var loadContext = new ModuleLoadContext(moduleFile.FullName);
                Assembly moduleAssembly = loadContext.LoadFromAssemblyPath(moduleFile.FullName);

                Type moduleType = moduleAssembly.GetTypes()
                    .FirstOrDefault(type => typeof(IModule).IsAssignableFrom(type));

                if (moduleType == null)
                {
                    continue;
                }

                if (Activator.CreateInstance(moduleType) is not IModule moduleInstance)
                {
                    continue;
                }

                builder.Configuration
                    .AddJsonFile(Path.Join(moduleFile.DirectoryName, "appsettings.json"), true)
                    .AddJsonFile(Path.Join(moduleFile.DirectoryName, $"appsettings.{builder.Environment.EnvironmentName}.json"), true);
                builder.Configuration.AddUserSecrets(moduleAssembly);

                moduleInstance.ConfigureServices(builder.Services, builder.Environment, builder.Configuration);

                builder.Services.AddSingleton(typeof(IModule), moduleInstance);

                yield return moduleInstance;
            }
        }
    }
}
