using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Whitestone.SegnoSharp.Configuration.Authentication
{
    // This is used to transform claims from the configured key to role claims.
    internal class RoleClaimsTransformation(IOptions<SegnoSharpOpenIdConnectOptions> options) : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            IEnumerable<Claim> currentRoleClaims = principal.FindAll(claim => claim.Type == options.Value.RoleClaim);

            ClaimsIdentity claimsIdentity = new();

            foreach (Claim currentRoleClaim in currentRoleClaims)
            {
                if (!principal.HasClaim(claim => claim.Type == ClaimTypes.Role &&
                                                 claim.Value == currentRoleClaim.Value))
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, currentRoleClaim.Value));
                }
            }

            principal.AddIdentity(claimsIdentity);

            return Task.FromResult(principal);
        }
    }
}
