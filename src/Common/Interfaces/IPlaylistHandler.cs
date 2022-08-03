using Microsoft.Extensions.Hosting;
using Whitestone.WASP.Common.Models;

namespace Whitestone.WASP.Common.Interfaces
{
    public interface IPlaylistHandler : IHostedService
    {
        Track GetNextTrack();
    }
}
