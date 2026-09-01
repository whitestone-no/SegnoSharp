using System.Collections.Immutable;

namespace Whitestone.SegnoSharp.Models.Security;

public sealed record RoleDefinition(int Id, string Name, ImmutableHashSet<string> Permissions);