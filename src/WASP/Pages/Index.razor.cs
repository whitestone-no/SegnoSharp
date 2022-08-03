using Microsoft.AspNetCore.Components;
using Whitestone.Cambion.Interfaces;
using Whitestone.WASP.Common.Events;
using Whitestone.WASP.Common.Interfaces;
using Whitestone.WASP.Common.Models;

namespace Whitestone.WASP.Pages
{
    public partial class Index
    {
        [Inject]
        private ICambion Cambion { get; set; } = null!;

        [Inject]
        private IPlaylistHandler PlaylistHandler { get; set; } = null!;

        private async void FireEvent_PlayNextTrack_Click()
        {
            Track track = PlaylistHandler.GetNextTrack();
            await Cambion.PublishEventAsync(new PlayTrack(track));
        }
        private async void FireEvent_StartStreaming_Click()
        {
            await Cambion.PublishEventAsync(new StartStreaming());
        }
        private async void FireEvent_StopStreaming_Click()
        {
            await Cambion.PublishEventAsync(new StopStreaming());
        }
    }
}
