using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Models.Persistent;

namespace Whitestone.SegnoSharp.Modules.StreamControls.Components.Pages
{
    public partial class StreamControls
    {
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private StreamingSettings Settings { get; set; }
        [Inject] private AudioSettings AudioSettings { get; set; }
        [Inject] private ILogger<StreamControls> Logger { get; set; }

        private byte _tempVolume;

        protected override void OnInitialized()
        {
            _tempVolume = AudioSettings.Volume;

            Settings.PropertyChanged += SettingsPropertyChanged;
        }

        private async void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{message}", ex.Message);
            }
        }

        private async Task FireEvent_PlayNextTrack_Click()
        {
            await Cambion.PublishEventAsync(new PlayNextTrack());
        }

        private async Task FireEvent_StartStreaming_Click()
        {
            await Cambion.PublishEventAsync(new StartStreaming());
        }

        private async Task FireEvent_StopStreaming_Click()
        {
            await Cambion.PublishEventAsync(new StopStreaming());
        }

        private void VolumeChanged()
        {
            AudioSettings.Volume = _tempVolume;

            Cambion.PublishEventAsync(new SetVolume(AudioSettings.Volume));
        }
    }
}
