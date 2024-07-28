using Whitestone.SegnoSharp.PersistenceManager.Attributes;

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
