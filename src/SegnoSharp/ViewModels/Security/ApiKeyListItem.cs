using System;

namespace Whitestone.SegnoSharp.ViewModels.Security
{
    public class ApiKeyListItem
    {
        public int Id { get; init; }
        public string Prefix { get; init; } = "";
        public string Description { get; init; }
        public DateTime Created { get; init; }
        public DateTime? Expires { get; init; }
        public DateTime? LastUsed { get; init; }
        public bool IsActive { get; init; }
        public bool ExpiresSoon { get; init; }
        public string Status { get; init; } = "";
    }
}
