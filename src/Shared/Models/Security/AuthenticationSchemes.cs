namespace Whitestone.SegnoSharp.Shared.Models.Security;

public static class AuthenticationSchemes
{
    public const string Cookie = "SegnoSharpAuthCookies";
    public const string Oidc = "SegnoSharpAuthOidc";
    public const string Bearer = "SegnoSharpAuthJwtBearer";
    public const string ApiKey = "SegnoSharpAuthApiKey";

    public const string All = Cookie + "," + Bearer + "," + ApiKey;
    public const string CookieOrBearer = Cookie + "," + Bearer;
}