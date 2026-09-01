using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Whitestone.SegnoSharp.Models.Security;

public sealed class SecurityRolesSnapshot
{
    public required FrozenDictionary<string, ImmutableArray<int>> ClaimToRoles { get; init; }
    public required FrozenDictionary<int, RoleDefinition> Roles { get; init; }

    public static SecurityRolesSnapshot Empty { get; } = new()
    {
        ClaimToRoles = FrozenDictionary<string, ImmutableArray<int>>.Empty,
        Roles = FrozenDictionary<int, RoleDefinition>.Empty
    };
}