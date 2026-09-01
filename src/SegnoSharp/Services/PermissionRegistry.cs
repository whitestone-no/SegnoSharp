using Microsoft.Extensions.Logging;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Security;
using Whitestone.SegnoSharp.Shared.Permissions;

namespace Whitestone.SegnoSharp.Services;

public sealed class PermissionRegistry
{
    public const string Wildcard = "*";

    private readonly FrozenDictionary<string, RegisteredPermission> _registeredPermissions;
    public IReadOnlyCollection<RegisteredPermission> All => _registeredPermissions.Values;

    public PermissionRegistry(IEnumerable<IPermissionProvider> providers, ILogger<PermissionRegistry> log)
    {
        Dictionary<string, RegisteredPermission> tempPermissions = new(StringComparer.Ordinal);

        // Use a list to avoid multiple enumerations.
        IEnumerable<IPermissionProvider> permissionProviders = providers.ToList();
        
        var corePermissions = permissionProviders.First(p => p is CorePermissions) as CorePermissions;

        foreach (IPermissionProvider provider in permissionProviders)
        {
            if (string.IsNullOrWhiteSpace(provider.PermissionPrefix))
            {
                throw new InvalidOperationException($"{provider.GetType().FullName} does not declare a PermissionPrefix.");
            }

            // ReSharper disable once PossibleNullReferenceException
            // `corePermissions` is known not to be null. If it is null then `.First()` above will throw an exception.
            if (provider is not CorePermissions && provider.PermissionPrefix.TrimEnd(':').Equals(corePermissions.PermissionPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Plugin '{provider.GetType().FullName}' uses prefix '{provider.PermissionPrefix}' which is reserved.");
            }

            string prefix = provider.PermissionPrefix.TrimEnd(':') + ":";

            foreach (Permission p in provider.ProvidedPermissions)
            {
                if (p.Name == Wildcard)
                {
                    throw new InvalidOperationException($"Plugin '{provider.GetType().FullName}' tried to register wildcard ('{Wildcard}') which is reserved.");
                }

                if (!p.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Permission '{p.Name}' from plugin '{provider.GetType().FullName}' must be prefixed with '{prefix}'.");
                }

                if (tempPermissions.TryGetValue(p.Name, out RegisteredPermission existing))
                {
                    throw new InvalidOperationException($"Permission '{p.Name}' is declared by both '{existing.PluginName}' and '{provider.GetType().FullName}'.");
                }

                tempPermissions[p.Name] = new RegisteredPermission { Permission = p, PluginName = provider.GetType().FullName };
            }
        }

        _registeredPermissions = tempPermissions.ToFrozenDictionary(StringComparer.Ordinal);

        log.LogInformation("Registered {Count} permissions from {Plugins} providers.", _registeredPermissions.Count, _registeredPermissions.Values.Select(p => p.PluginName).Distinct().Count());
    }


    public bool Contains(string name) => _registeredPermissions.ContainsKey(name);

    public RegisteredPermission Find(string name) => _registeredPermissions.TryGetValue(name, out RegisteredPermission p) ? p : null;

    public IEnumerable<IGrouping<string, RegisteredPermission>> ByPlugin() =>
        _registeredPermissions.Values
            .OrderBy(p => p.PluginName == typeof(CorePermissions).FullName ? 0 : 1)
            .ThenBy(p => p.PluginName, StringComparer.Ordinal)
            .GroupBy(p => p.PluginName);
}