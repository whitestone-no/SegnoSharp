namespace Whitestone.SegnoSharp.Shared.Models.Security;

public static class AuthenticationSchemes
{
    public const string Cookie = "SegnoSharpAuthCookies";
    public const string Oidc = "SegnoSharpAuthOidc";
    public const string Bearer = "SegnoSharpAuthJwtBearer";

    public const string CookieOrBearer = Cookie + "," + Bearer;
}