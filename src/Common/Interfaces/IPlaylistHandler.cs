using System.Threading;
using Microsoft.Extensions.Hosting;
using Whitestone.SegnoSharp.Common.Models;

namespace Whitestone.SegnoSharp.Common.Interfaces
{
    public interface IPlaylistHandler : IHostedService
    {
        Track GetNextTrack(CancellationToken cancellationToken = default);
    }
}
