using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using Whitestone.SegnoSharp.Modules.AppInspector.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Whitestone.SegnoSharp.Modules.AppInspector.Extensions;

namespace Whitestone.SegnoSharp.Modules.AppInspector.Components.Pages
{
    public partial class AppInspector
    {
        [Inject] private IConfiguration Configuration { get; set; }
        [Inject] private IServiceProvider ServiceProvider { get; set; }

        private List<ConfigurationViewModel> _configurations = [];
        private List<DependencyViewModel> _dependencies = [];

        protected override void OnInitialized()
        {
            _configurations = AllConfigurations(Configuration);
            _dependencies = AllDependencies(ServiceProvider);
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

        private List<DependencyViewModel> AllDependencies(IServiceProvider provider)
        {
            var rootProvider = provider.GetType().GetProperty("RootProvider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(provider);
            var callSiteFactory = rootProvider.GetType().GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rootProvider);
            var serviceDescriptors = callSiteFactory.GetType().GetProperty("Descriptors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(callSiteFactory) as ServiceDescriptor[];

            return serviceDescriptors
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
        }
    }
}
