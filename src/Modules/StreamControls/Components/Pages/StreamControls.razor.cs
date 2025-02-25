using Microsoft.AspNetCore.Components;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Common.Events;
using Whitestone.SegnoSharp.Modules.StreamControls.Models;

namespace Whitestone.SegnoSharp.Modules.StreamControls.Components.Pages
{
    public partial class StreamControls
    {
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private Settings Settings { get; set; }

        // ReSharper disable AsyncVoidMethod
        private async void FireEvent_PlayNextTrack_Click()
        {
            await Cambion.PublishEventAsync(new PlayNextTrack());
        }

        private async void FireEvent_StartStreaming_Click()
        {
            await Cambion.PublishEventAsync(new StartStreaming(Settings));
        }

        private async void FireEvent_StopStreaming_Click()
        {
            await Cambion.PublishEventAsync(new StopStreaming());
        }
        // ReSharper restore AsyncVoidMethod
    }
}
