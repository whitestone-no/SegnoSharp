using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Modules.Playlist.Models
{
    public class PlaylistSettings
    {
        [Persist]
        [DefaultValue(3)]
        [Description("Minimum number of songs")]
        public ushort MinimumNumberOfSongs { get; set; }

        [Persist]
        [DefaultValue(15)]
        [Description("Minimum total duration")]
        public ushort MinimumTotalDuration { get; set; }

    }
}
