using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Whitestone.SegnoSharp.Pages.Admin
{
    public partial class Debug
    {
        [Inject] private IServiceCollection ServiceCollection { get; set; }

        private List<DependencyViewModel> _dependencies = new();

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
        }

        private class DependencyViewModel
        {
            public string Lifetime { get; init; }
            public string ServiceType { get; init; }
            public string ImplementationType { get; init; }
        }
    }

    internal static class TypeExtensions
    {
        internal static string GetTypeName(this Type type)
        {
            string typeName = type.Namespace + "." + type.Name;
            if (typeName.Contains('`'))
            {
                typeName = typeName[..typeName.LastIndexOf('`')];
            }

            if (type.IsGenericType)
            {
                typeName += "<";

                if (type.GenericTypeArguments.Any())
                {
                    typeName += type.GenericTypeArguments[0].GetTypeName();
                }
                typeName += ">";
            }

            return typeName;
        }
    }
}