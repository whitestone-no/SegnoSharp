using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Common.Interfaces
{
    public interface IPlaylistProcessor
    {
        PlaylistProcessorSettings Settings { get; set; }

        Task<TrackStreamInfo> GetNextTrackAsync(CancellationToken cancellationToken);
    }
}
