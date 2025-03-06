using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Shared.Models
{
    public abstract class PlaylistProcessorSettings
    {
        [Persist]
        [DefaultValue(false)]
        public virtual bool Enabled { get; set; }

        [Persist]
        public ushort SortOrder { get; set; }
    }
}
