using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Shared.Attributes.Security;

namespace Whitestone.SegnoSharp.Configuration.Authentication;

public sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy> GetPolicyAsync(string name)
    {
        if (!name.StartsWith(RequirePermissionAttribute.Prefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(name);
        }

        string permission = name[RequirePermissionAttribute.Prefix.Length..];

        return string.IsNullOrEmpty(permission)
            ? null
            : new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
    }
}