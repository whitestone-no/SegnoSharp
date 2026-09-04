using Microsoft.AspNetCore.Authorization;
using Whitestone.SegnoSharp.Shared.Helpers.Security;

namespace Whitestone.SegnoSharp.Shared.Attributes.Security;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string Prefix = "perm:";
    public const char AnySeparator = '|';
    public const char AllSeparator = '&';

    public RequirePermissionAttribute(params string[] permissions)
    {
        Policy = PermissionPolicy.ForAny(permissions);
    }
}