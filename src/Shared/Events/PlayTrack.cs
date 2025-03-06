using Whitestone.SegnoSharp.Shared.Models;

namespace Whitestone.SegnoSharp.Shared.Events
{
    public class PlayTrack
    {
        public Track Track { get; }

        public PlayTrack(Track track)
        {
            Track = track;
        }
    }
}
