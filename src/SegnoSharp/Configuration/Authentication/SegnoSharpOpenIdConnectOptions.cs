namespace Whitestone.SegnoSharp.Configuration.Authentication
{
    public class SegnoSharpOpenIdConnectOptions
    {
        public const string Section = "OpenIdConnect";

        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AdditionalScopes { get; set; }
        public bool SupportsEndSession { get; set; }
        public string RoleClaim { get; set; }
        public string AdminRole { get; set; }
        public string UsernameClaimKey { get; set; }
        public bool UseOidc { get; set; }
    }
}
