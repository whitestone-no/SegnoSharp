using Microsoft.AspNetCore.Components;
using Whitestone.Cambion.Interfaces;
using Whitestone.WASP.Common.Events;

namespace Whitestone.WASP.Pages
{
    public partial class Index
    {
        [Inject]
        private ICambion Cambion { get; set; } = null!;

        private async void FireEvent_PlayNextTrack_Click()
        {
            await Cambion.PublishEventAsync(new PlayNextTrack());
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
