using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Shared.Attributes.Security;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Configuration.Authentication;

public sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy> GetPolicyAsync(string name)
    {
        if (!name.StartsWith(RequirePermissionAttribute.Prefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(name);
        }

        string spec = name[RequirePermissionAttribute.Prefix.Length..];

        bool all = spec.Contains(RequirePermissionAttribute.AllSeparator);

        // Mixing the separators has no defined precedence, so refuse rather than guess.
        // Returning null fails the request, which is the correct outcome for a malformed name.
        if (all && spec.Contains(RequirePermissionAttribute.AnySeparator))
        {
            return null;
        }

        char separator = all
            ? RequirePermissionAttribute.AllSeparator
            : RequirePermissionAttribute.AnySeparator;

        string[] permissions = spec.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        return permissions.Length == 0
            ? null
            : new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(
                    permissions, all ? PermissionMatch.All : PermissionMatch.Any))
                .Build();
    }
}