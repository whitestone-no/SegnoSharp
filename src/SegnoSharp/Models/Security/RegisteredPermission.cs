using System;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Models.Security
{
    public sealed class RegisteredPermission
    {
        public string PluginName { get; init; }
        public Permission Permission { get; init; }

        public string Name => Permission.Name;
    }
}
