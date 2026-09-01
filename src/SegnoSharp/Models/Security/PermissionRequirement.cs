using Microsoft.AspNetCore.Authorization;

namespace Whitestone.SegnoSharp.Models.Security;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;