using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Playlist.Models
{
    public class PlaylistSettings
    {
        [Persist]
        [DefaultValue(3)]
        public int MinimumNumberOfSongs { get; set; }

        [Persist]
        [DefaultValue(15)]
        public int MinimumTotalDuration { get; set; }
    }
}
