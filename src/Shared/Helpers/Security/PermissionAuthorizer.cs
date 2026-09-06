using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Whitestone.SegnoSharp.Shared.Helpers.Security;

/// <summary>
/// Evaluates permission policies for an explicit principal. Use this outside Blazor, where
/// the principal comes from HttpContext.User or an injected ClaimsPrincipal.
/// </summary>
public sealed class PermissionAuthorizer(IAuthorizationService authorizationService)
{
    public async Task<bool> HasAnyAsync(ClaimsPrincipal user, params string[] permissions)
    {
        AuthorizationResult authorizationResult = await authorizationService.AuthorizeAsync(user, PermissionPolicy.ForAny(permissions));
        
        return authorizationResult.Succeeded;
    }

    public async Task<bool> HasAllAsync(ClaimsPrincipal user, params string[] permissions)
    {
        AuthorizationResult authorizationResult = await authorizationService.AuthorizeAsync(user, PermissionPolicy.ForAll(permissions));

        return authorizationResult.Succeeded;
    }
}