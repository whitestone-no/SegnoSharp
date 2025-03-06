using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Modules.Playlist.Models
{
    public class PlaylistSettings
    {
        [Persist]
        [DefaultValue(3)]
        public ushort MinimumNumberOfSongs { get; set; }

        [Persist]
        [DefaultValue(15)]
        public ushort MinimumTotalDuration { get; set; }

    }
}
