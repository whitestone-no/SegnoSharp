using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Common.Events;
using Whitestone.SegnoSharp.Common.Models.Persistent;
using Whitestone.SegnoSharp.Modules.StreamControls.Models;

namespace Whitestone.SegnoSharp.Modules.StreamControls.Components.Pages
{
    public partial class StreamControls
    {
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private Settings Settings { get; set; }
        [Inject] private AudioSettings AudioSettings { get; set; }
        [Inject] private ILogger<StreamControls> Logger { get; set; }

        private byte _tempVolume;

        protected override void OnInitialized()
        {
            _tempVolume = AudioSettings.Volume;
        }

        private async Task FireEvent_PlayNextTrack_Click()
        {
            await Cambion.PublishEventAsync(new PlayNextTrack());
        }

        private async Task FireEvent_StartStreaming_Click()
        {
            await Cambion.PublishEventAsync(new StartStreaming(Settings));

            Settings.IsStreaming = true;
        }

        private async Task FireEvent_StopStreaming_Click()
        {
            await Cambion.PublishEventAsync(new StopStreaming());

            Settings.IsStreaming = false;
        }

        private void VolumeChanged()
        {
            AudioSettings.Volume = _tempVolume;

            Cambion.PublishEventAsync(new SetVolume(AudioSettings.Volume));
        }
    }
}
