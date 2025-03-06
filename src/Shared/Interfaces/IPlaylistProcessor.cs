using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Shared.Models;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Shared.Interfaces
{
    public interface IPlaylistProcessor
    {
        string Name { get; }
        PlaylistProcessorSettings Settings { get; set; }

        Task<TrackStreamInfo> GetNextTrackAsync(CancellationToken cancellationToken);
    }
}
