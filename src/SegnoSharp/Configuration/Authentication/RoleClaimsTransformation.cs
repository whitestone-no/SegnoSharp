using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Configuration.Models;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Services;

namespace Whitestone.SegnoSharp.Configuration.Authentication
{
    // This is used to transform claims from the configured key to role claims.
    internal class RoleClaimsTransformation(
        IOptions<SegnoSharpOpenIdConnectOptions> options,
        SecurityRolesSnapshotProvider snapshots,
        ILogger<RoleClaimsTransformation> log) : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Runs on every authentication call, which can be more than once per request.
            if (principal.HasClaim(c => c.Type == Constants.PermissionsClaim))
            {
                return Task.FromResult(principal);
            }

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

            return Task.FromResult(principal);
        }
    }
}
