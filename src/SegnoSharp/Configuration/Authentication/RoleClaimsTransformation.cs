using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Configuration.Models;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Services;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Configuration.Authentication
{
    // This is used to transform claims from the configured key to role claims.
    internal class RoleClaimsTransformation(
        IOptions<SegnoSharpOpenIdConnectOptions> options,
        SecurityRolesSnapshotProvider snapshots,
        UnmappedRoleClaimTracker unmappedRoleClaimTracker,
        PermissionRegistry permissionRegistry,
        ApiClientGrantStore grantStore,
        ILogger<RoleClaimsTransformation> log) : IClaimsTransformation
    {
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Runs on every authentication call, which can be more than once per request.
            if (principal.HasClaim(c => c.Type == Constants.PermissionsClaim))
            {
                return principal;
            }

            string scheme = principal.FindFirst(Constants.AuthenticationSchemeClaim)?.Value ?? AuthenticationSchemes.Cookie;


            return scheme == AuthenticationSchemes.ApiKey
                ? await TransformApiClientAsync(principal)
                : TransformUser(principal);
        }

        private ClaimsPrincipal TransformUser(ClaimsPrincipal principal)
        {
            SecurityRolesSnapshot snapshot = snapshots.Current;
            Dictionary<int, RoleDefinition> roles = new();
            HashSet<string> seenClaimValues = new(StringComparer.OrdinalIgnoreCase);
            List<string> unmapped = null;

            IEnumerable<Claim> currentRoleClaims = principal.FindAll(claim => claim.Type == options.Value.RoleClaim);

            foreach (Claim currentRoleClaim in currentRoleClaims)
            {
                if (!seenClaimValues.Add(currentRoleClaim.Value))
                {
                    continue;
                }

                if (!snapshot.ClaimToRoles.TryGetValue(currentRoleClaim.Value, out ImmutableArray<int> ids))
                {
                    unmappedRoleClaimTracker.Record(currentRoleClaim.Value);
                    (unmapped ??= []).Add(currentRoleClaim.Value);
                    continue;
                }

                foreach (int id in ids)
                {
                    if (snapshot.Roles.TryGetValue(id, out RoleDefinition role))
                    {
                        roles[id] = role;
                    }
                }
            }

            ClaimsIdentity claimsIdentity = new();

            foreach (string permission in roles.Values
                         .SelectMany(r => r.Permissions)
                         .Distinct(StringComparer.Ordinal))
            {
                claimsIdentity.AddClaim(new Claim(Constants.PermissionsClaim, permission));
            }

            foreach (RoleDefinition role in roles.Values)
            {
                claimsIdentity.AddClaim(new Claim(Constants.RolesClaim, role.Name));

                // Internal role name, not the IdP identifier — makes User.IsInRole("admin")
                // and [Authorize(Roles = "admin")] behave as a developer would expect.
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
            }

            // Sentinel so re-entry short-circuits even when nothing matched. Without it,
            // an unmapped user re-runs this on every authentication call and accumulates identities.
            if (roles.Count == 0)
            {
                // Marker so re-entry short-circuits even when no roles matched.
                claimsIdentity.AddClaim(new Claim(Constants.PermissionsClaim, string.Empty));
            }

            if (unmapped is not null)
            {
                // Opaque IdP identifiers are hard for an operator to discover; surface them.
                log.LogDebug("Unmapped role claim values on {Scheme}: {Values}",
                    principal.Identity?.AuthenticationType,
                    string.Join(", ", unmapped));
            }

            principal.AddIdentity(claimsIdentity);

            return principal;
        }

        private async Task<ClaimsPrincipal> TransformApiClientAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity claimsIdentity = new();

            string rawClientId = principal.FindFirst(Constants.ClientIdClaim)?.Value;

            if (int.TryParse(rawClientId, out int clientId))
            {
                ImmutableHashSet<string> permissions = await grantStore.GetPermissionsAsync(clientId);

                foreach (string permission in permissions)
                {
                    // Enforced at expansion, not just in the admin UI: a grant that predates
                    // the flag, or one written directly to the database, contributes nothing.
                    // Unknown permissions default to allowed so orphaned grants keep working.
                    if (permissionRegistry.Find(permission)?.Permission.AllowForApiClients ?? true)
                    {
                        claimsIdentity.AddClaim(new Claim(Constants.PermissionsClaim, permission));
                    }
                }
            }

            // Sentinel so re-entry short-circuits when nothing matched.
            if (!claimsIdentity.HasClaim(c => c.Type == Constants.PermissionsClaim))
            {
                claimsIdentity.AddClaim(new Claim(Constants.PermissionsClaim, string.Empty));
            }

            principal.AddIdentity(claimsIdentity);

            return principal;
        }
    }
}
