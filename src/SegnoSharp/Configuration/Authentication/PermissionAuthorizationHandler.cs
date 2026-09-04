using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Configuration.Models;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Configuration.Authentication;

public sealed class PermissionAuthorizationHandler
    (IOptions<SegnoSharpOpenIdConnectOptions> options)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext ctx, PermissionRequirement req)
    {
        // Guards against the sentinel claim written when nothing matched.
        if (req.Permissions.Length == 0)
        {
            return Task.CompletedTask;
        }

        if (IsBearerRequest(ctx.User))
        {
            string requiredScope = options.Value.JwtScope;

            if (!string.IsNullOrEmpty(requiredScope) && !HasScope(ctx.User, requiredScope))
            {
                return Task.CompletedTask;
            }
        }

        bool granted =
            ctx.User.HasClaim(Constants.PermissionsClaim, "*") ||
            (req.Match == PermissionMatch.All
                ? req.Permissions.All(HasPermission)
                : req.Permissions.Any(HasPermission));

        if (granted)
        {
            ctx.Succeed(req);
        }

        return Task.CompletedTask;

        bool HasPermission(string permission)
        {
            return ctx.User.HasClaim(Constants.PermissionsClaim, permission);
        }
    }

    // Identity.AuthenticationType is "AuthenticationTypes.Federation" for both cookie
    // and bearer principals, so the scheme is carried on an explicit claim instead.
    private static bool IsBearerRequest(ClaimsPrincipal user) =>
        string.Equals(
            user.FindFirst(Constants.AuthenticationSchemeClaim)?.Value,
            AuthenticationSchemes.Bearer,
            StringComparison.Ordinal);

    // Some providers emits one claim per scope; other providers emit a single space-delimited value.
    private static bool HasScope(ClaimsPrincipal user, string scope) =>
        user.FindAll("scope").Any(c =>
            c.Value == scope ||
            c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Contains(scope, StringComparer.Ordinal));
}