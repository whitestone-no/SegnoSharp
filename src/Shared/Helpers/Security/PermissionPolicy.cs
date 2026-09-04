using Whitestone.SegnoSharp.Shared.Attributes.Security;

namespace Whitestone.SegnoSharp.Shared.Helpers.Security;

public static class PermissionPolicy
{
    /// <summary>Satisfied by holding the permission.</summary>
    public static string For(string permission) => ForAny(permission);

    /// <summary>Satisfied by holding any one of the permissions.</summary>
    public static string ForAny(params string[] permissions) =>
        RequirePermissionAttribute.Prefix +
        string.Join(RequirePermissionAttribute.AnySeparator, permissions);

    /// <summary>Satisfied only by holding all of the permissions.</summary>
    public static string ForAll(params string[] permissions) =>
        RequirePermissionAttribute.Prefix +
        string.Join(RequirePermissionAttribute.AllSeparator, permissions);
}