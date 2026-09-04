using Microsoft.AspNetCore.Authorization;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Models.Security;

public sealed record PermissionRequirement(string[] Permissions, PermissionMatch Match) : IAuthorizationRequirement;