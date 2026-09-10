using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Whitestone.SegnoSharp.Services;

namespace Whitestone.SegnoSharp.Configuration.Extensions
{
    public static partial class McpServerBuilderExtensions
    {
        [GeneratedRegex(@"^[A-Za-z0-9_.-]{1,64}$")]
        private static partial Regex McpPrefixRegex();

        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance;

        /// <summary>
        /// Returns true if <paramref name="prefix"/> is usable as an MCP tool-name prefix.
        /// Capped at 64 chars to leave room for the member name under the SDK's 128 limit.
        /// </summary>
        public static bool IsValidMcpPrefix([NotNullWhen(true)] string prefix)
        {
            return !string.IsNullOrWhiteSpace(prefix) && McpPrefixRegex().IsMatch(prefix);
        }

        /// <summary>
        /// Scans <paramref name="assembly"/> for <see cref="McpServerToolTypeAttribute"/> types and
        /// registers their <see cref="McpServerToolAttribute"/> methods, renaming each tool via
        /// <paramref name="nameFactory"/>.
        /// </summary>
        /// <param name="nameFactory">
        /// Receives the declaring type, the method, and the name the SDK derived (snake_case, with any
        /// <c>Async</c> suffix stripped), and returns the name to expose. Results must match
        /// <c>^[A-Za-z0-9_.-]{1,128}$</c>.
        /// </param>
        [RequiresUnreferencedCode("Scans plugin assemblies via reflection.")]
        public static IMcpServerBuilder WithPrefixedToolsFromAssembly(
            this IMcpServerBuilder builder,
            Assembly assembly,
            Func<string, string> nameFactory,
            Action<Exception, string> onLoadError = null,
            JsonSerializerOptions serializerOptions = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(nameFactory);

            List<(Type DeclaringType, MethodInfo Method)> members = Scan<McpServerToolTypeAttribute, McpServerToolAttribute>(assembly, onLoadError);

            foreach ((Type declaringType, MethodInfo toolMethod) in members)
            {
                builder.Services.AddSingleton<McpServerTool>(services =>
                {
                    var options = new McpServerToolCreateOptions
                    {
                        Services = services,
                        SerializerOptions = serializerOptions,
                    };

                    McpServerTool tool = toolMethod.IsStatic
                        ? McpServerTool.Create(toolMethod, options: options)
                        : McpServerTool.Create(
                            toolMethod,
                            r => ActivatorUtilities.CreateInstance(r.Services!, declaringType),
                            options);

                    Tool protocolTool = tool.ProtocolTool;
                    protocolTool.Name = nameFactory(protocolTool.Name);
                    return tool;
                });
            }

            return builder;
        }

        /// <summary>
        /// Prompt equivalent of <see cref="WithPrefixedToolsFromAssembly"/>. Prompts use the same name
        /// derivation and the same name-keyed collection as tools, so they collide the same way.
        /// </summary>
        [RequiresUnreferencedCode("Scans plugin assemblies via reflection.")]
        public static IMcpServerBuilder WithPrefixedPromptsFromAssembly(
            this IMcpServerBuilder builder,
            Assembly assembly,
            Func<string, string> nameFactory,
            Action<Exception, string> onLoadError = null,
            JsonSerializerOptions serializerOptions = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(nameFactory);

            List<(Type DeclaringType, MethodInfo Method)> members = Scan<McpServerPromptTypeAttribute, McpServerPromptAttribute>(assembly, onLoadError);

            foreach ((Type declaringType, MethodInfo promptMethod) in members)
            {
                builder.Services.AddSingleton<McpServerPrompt>(services =>
                {
                    var options = new McpServerPromptCreateOptions
                    {
                        Services = services,
                        SerializerOptions = serializerOptions,
                    };

                    McpServerPrompt prompt = promptMethod.IsStatic
                        ? McpServerPrompt.Create(promptMethod, options: options)
                        : McpServerPrompt.Create(
                            promptMethod,
                            r => ActivatorUtilities.CreateInstance(r.Services!, declaringType),
                            options);

                    Prompt protocolPrompt = prompt.ProtocolPrompt;
                    protocolPrompt.Name = nameFactory(protocolPrompt.Name);
                    return prompt;
                });
            }

            return builder;
        }

        /// <summary>
        /// Registers resources from <paramref name="assembly"/> without renaming them. Resources are keyed
        /// by URI template rather than name, and the template is baked into a precomputed matcher at
        /// construction, so rewriting it afterwards would list the resource under one URI while it still
        /// matches reads against another. Instead, when <paramref name="requiredUriScheme"/> is supplied,
        /// each resource must already declare a URI under that scheme.
        /// </summary>
        [RequiresUnreferencedCode("Scans plugin assemblies via reflection.")]
        public static IMcpServerBuilder WithScopedResourcesFromAssembly(
            this IMcpServerBuilder builder,
            Assembly assembly,
            string requiredUriScheme = null,
            Action<Exception, string> onLoadError = null,
            JsonSerializerOptions serializerOptions = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(assembly);

            List<(Type DeclaringType, MethodInfo Method)> members = Scan<McpServerResourceTypeAttribute, McpServerResourceAttribute>(assembly, onLoadError);

            foreach ((Type declaringType, MethodInfo resourceMethod) in members)
            {
                builder.Services.AddSingleton<McpServerResource>(services =>
                {
                    var options = new McpServerResourceCreateOptions
                    {
                        Services = services,
                        SerializerOptions = serializerOptions,
                    };

                    McpServerResource resource = resourceMethod.IsStatic
                        ? McpServerResource.Create(resourceMethod, options: options)
                        : McpServerResource.Create(
                            resourceMethod,
                            r => ActivatorUtilities.CreateInstance(r.Services!, declaringType),
                            options);

                    if (requiredUriScheme is null)
                    {
                        return resource;
                    }

                    string uri = resource.ProtocolResourceTemplate.UriTemplate;

                    if (!uri.StartsWith($"{requiredUriScheme}://", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Resource '{uri}' on {declaringType.FullName} must declare a UriTemplate " +
                            $"under the '{requiredUriScheme}://' scheme.");
                    }

                    return resource;
                });
            }

            return builder;
        }

        [RequiresUnreferencedCode("Scans plugin assemblies via reflection.")]
        private static List<(Type DeclaringType, MethodInfo Method)> Scan<TTypeAttribute, TMemberAttribute>(
            Assembly assembly,
            Action<Exception, string> onLoadError)
            where TTypeAttribute : Attribute
            where TMemberAttribute : Attribute
        {
            var members = new List<(Type, MethodInfo)>();

            foreach (Type type in GetLoadableTypes(assembly, onLoadError))
            {
                if (type.GetCustomAttribute<TTypeAttribute>() is null)
                {
                    continue;
                }

                foreach (MethodInfo method in type.GetMethods(MemberFlags))
                {
                    if (method.GetCustomAttribute<TMemberAttribute>() is not null)
                    {
                        members.Add((type, method));
                    }
                }
            }

            return members;
        }

        /// <summary>
        /// Fails startup when two registrations would collide. The SDK folds primitives into their
        /// collections with a discarded <c>TryAdd</c>, so without this a duplicate is silently dropped.
        /// </summary>
        public static IMcpServerBuilder ValidateUniquePrimitiveNames(this IMcpServerBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton<IValidateOptions<McpServerOptions>, UniqueMcpNameValidator>();
            return builder;
        }

        private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly, Action<Exception, string> onLoadError)
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
