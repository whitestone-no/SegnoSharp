using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Whitestone.SegnoSharp.Configuration.Extensions
{
    public static partial class McpServerBuilderExtensions
    {
        [GeneratedRegex(@"^[A-Za-z0-9_.-]{1,64}$")]
        private static partial Regex McpPrefixRegex();

        private const BindingFlags MethodFlags =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance;

        /// <summary>
        /// Returns true if <paramref name="prefix"/> is usable as an MCP tool-name prefix.
        /// Capped at 64 chars to leave room for the method name under the SDK's 128 limit.
        /// </summary>
        public static bool IsValidMcpPrefix([NotNullWhen(true)] string prefix)
        {
            return !string.IsNullOrWhiteSpace(prefix) && McpPrefixRegex().IsMatch(prefix);
        }

        [RequiresUnreferencedCode("Scans plugin assemblies via reflection.")]
        public static IMcpServerBuilder WithPrefixedToolsFromAssembly(
            this IMcpServerBuilder builder,
            Assembly assembly,
            Func<string, string> nameFactory,
            Action<Exception, string> onLoadError = null,
            JsonSerializerOptions serializerOptions = null)

        {
            foreach (Type toolType in GetLoadableTypes(assembly, onLoadError))
            {
                if (toolType.GetCustomAttribute<McpServerToolTypeAttribute>() is null)
                {
                    continue;
                }

                foreach (MethodInfo method in toolType.GetMethods(MethodFlags))
                {
                    if (method.GetCustomAttribute<McpServerToolAttribute>() is null)
                    {
                        continue;
                    }

                    builder.Services.AddSingleton<McpServerTool>(services =>
                    {
                        var options = new McpServerToolCreateOptions
                        {
                            Services = services,
                            SerializerOptions = serializerOptions,
                        };

                        McpServerTool tool = method.IsStatic
                            ? McpServerTool.Create(method, options: options)
                            : McpServerTool.Create(
                                method,
                                r => ActivatorUtilities.CreateInstance(r.Services!, toolType),
                                options);

                        Tool protocolTool = tool.ProtocolTool;
                        protocolTool.Name = nameFactory(protocolTool.Name);
                        
                        return tool;
                    });
                }
            }

            return builder;
        }

        private static IReadOnlyList<Type> GetLoadableTypes(
            Assembly assembly,
            Action<Exception, string> onLoadError)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (onLoadError is not null)
                {
                    string assemblyName = assembly.FullName ?? assembly.ToString();

                    foreach (Exception loaderException in ex.LoaderExceptions)
                    {
                        if (loaderException is not null)
                        {
                            onLoadError(loaderException, assemblyName);
                        }
                    }
                }

                var loaded = new List<Type>(ex.Types.Length);
                loaded.AddRange(ex.Types.Where(type => type is not null));

                return loaded;
            }
        }
    }
}
