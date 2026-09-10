using System.Collections.Generic;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Shared.Interfaces;

public interface IPermissionProvider : IModule
{
    string PermissionPrefix { get; }

    IEnumerable<Permission> ProvidedPermissions { get; }
}