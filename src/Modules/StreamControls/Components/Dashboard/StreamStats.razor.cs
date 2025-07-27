using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Timers;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Persistent;

namespace Whitestone.SegnoSharp.Modules.StreamControls.Components.Dashboard
{
    public partial class StreamStats : IDashboardBox
    {
        public static string Name => "Stream statistics";
        public static string Title => "Stream statistics";
        public static string AdditionalCss => $"_moduleresource/{typeof(StreamStats).Assembly.GetName().Name}/dashboard.css";

        [Inject] private StreamingSettings StreamingSettings { get; set; }
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private ILogger<StreamStats> Logger { get; set; }

        private int _listeners;
        private int _peakListeners;

        private Timer _timer;

        protected override void OnInitialized()
        {
            _timer = new Timer(TimeSpan.FromSeconds(5));
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!StreamingSettings.IsStreaming)
                {
                    return;
                }

                GetListenersResponse listeners = await Cambion.CallSynchronizedHandlerAsync<GetListenersRequest, GetListenersResponse>(new GetListenersRequest());

                _listeners = listeners.Listeners;
                _peakListeners = listeners.PeakListeners;

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during timer event: {message}", ex.Message);
            }
        }
    }
}
