using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Common.Models
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
