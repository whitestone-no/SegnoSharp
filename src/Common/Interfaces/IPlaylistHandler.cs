using Whitestone.WASP.Common.Models;

namespace Whitestone.WASP.Common.Interfaces
{
    public interface IPlaylistHandler
    {
        Track GetNextTrack();
    }
}
