using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Common.Models.Configuration;

namespace Whitestone.SegnoSharp.Pages.Admin
{
    public partial class Debug
    {
        [Inject] private IServiceCollection ServiceCollection { get; set; }
        [Inject] private IConfiguration Configuration { get; set; }
        [Inject] private IOptions<CommonConfig> CommonConfig { get; set; }

        private List<DependencyViewModel> _dependencies = new();
        private List<ConfigurationViewModel> _configurations = new();

        protected override void OnInitialized()
        {
            _dependencies = ServiceCollection
                .Select(s => new DependencyViewModel
                {
                    ServiceType = s.ServiceType.GetTypeName(),
                    ImplementationType = s.ImplementationType?.GetTypeName(),
                    Lifetime = s.Lifetime.ToString()
                })
                .OrderBy(d => d.Lifetime)
                .ThenBy(d => d.ServiceType)
                .ThenBy(d => d.ImplementationType)
                .ToList();

            _configurations = AllConfigurations(Configuration);
        }

        private static List<ConfigurationViewModel> AllConfigurations(IConfiguration root)
        {
            var configurations = new List<ConfigurationViewModel>();
            RecurseChildren(configurations, root.GetChildren(), "");
            return configurations.ToList();

            void RecurseChildren(
                ICollection<ConfigurationViewModel> innerConfigurations,
                IEnumerable<IConfigurationSection> children, string rootPath)
            {
                foreach (IConfigurationSection child in children)
                {
                    (string value, IConfigurationProvider provider) = GetValueAndProvider((IConfigurationRoot)root, child.Path);

                    if (provider != null)
                    {
                        innerConfigurations.Add(new ConfigurationViewModel
                        {
                            Key = (rootPath + ":" + child.Key).TrimStart(':'),
                            Value = value,
                            Provider = provider.ToString()
                        });
                    }

                    RecurseChildren(innerConfigurations, child.GetChildren(), child.Path);
                }
            }

            (string Value, IConfigurationProvider Provider) GetValueAndProvider(
                IConfigurationRoot innerRoot,
                string key)
            {
                foreach (IConfigurationProvider provider in innerRoot.Providers.Reverse())
                {
                    if (provider.TryGet(key, out string value))
                    {
                        return (value, provider);
                    }
                }

                return (null, null);
            }
        }

        private class DependencyViewModel
        {
            public string Lifetime { get; init; }
            public string ServiceType { get; init; }
            public string ImplementationType { get; init; }
        }

        private class ConfigurationViewModel
        {
            public string Key { get; init; }
            public string Value { get; init; }
            public string Provider { get; init; }
        }
    }

    internal static class CustomExtensions
    {
        internal static string GetTypeName(this Type type)
        {
            string typeName = type.Namespace + "." + type.Name;
            if (typeName.Contains('`'))
            {
                typeName = typeName[..typeName.LastIndexOf('`')];
            }

            if (!type.IsGenericType)
            {
                return typeName;
            }

            typeName += "<";

            if (type.GenericTypeArguments.Any())
            {
                typeName += type.GenericTypeArguments[0].GetTypeName();
            }
            
            typeName += ">";

            return typeName;
        }
    }
}