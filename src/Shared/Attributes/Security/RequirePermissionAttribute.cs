using Microsoft.AspNetCore.Authorization;

namespace Whitestone.SegnoSharp.Shared.Attributes.Security;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string Prefix = "perm:";
    public RequirePermissionAttribute(string permission) => Policy = Prefix + permission;
}