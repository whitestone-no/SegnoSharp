using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Configuration.Models
{
    public class SegnoSharpOpenIdConnectOptions
    {
        public const string Section = "OpenIdConnect";

        public string Authority { get; set; }
        public string JwtAudience { get; set; }
        public string JwtScope { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AdditionalScopes { get; set; }
        public bool SupportsEndSession { get; set; }
        public string RoleClaim { get; set; }
        public List<string> AdminRole { get; set; }
        public string UsernameClaimKey { get; set; }
        public bool UseOidc { get; set; }
    }
}
