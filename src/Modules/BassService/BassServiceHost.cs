using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Un4seen.Bass;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Common.Events;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Modules.BassService.Interfaces;
using Whitestone.SegnoSharp.Modules.BassService.Models;
using Whitestone.SegnoSharp.Modules.BassService.Models.Config;

namespace Whitestone.SegnoSharp.Modules.BassService
{
    public class BassServiceHost : IHostedService, IEventHandler<PlayTrack>, IEventHandler<StartStreaming>, IEventHandler<StopStreaming>
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly CommonConfig _commonConfig;
        private readonly ICambion _cambion;
        private readonly ILogger<BassServiceHost> _log;

        private int _mixer;
        private TrackExt _currentlyPlayingTrack;

        public BassServiceHost(IBassWrapper bassWrapper,
            IOptions<BassRegistration> bassRegistration,
            IOptions<CommonConfig> commonConfig,
            ICambion cambion,
            ILogger<BassServiceHost> log)
        {
            _bassWrapper = bassWrapper;
            _commonConfig = commonConfig.Value;
            _cambion = cambion;
            _log = log;

            _cambion.Register(this);

            _bassWrapper.Registration(bassRegistration.Value.Email, bassRegistration.Value.Key);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                LoadAssemblies();

                // Initialize BASS
                if (!_bassWrapper.Initialize(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
                {
                    _log.LogCritical("Could not initialize BASS.NET: {bassError}", _bassWrapper.GetLastBassError());
                }

                // Initialize BASS Mixer
                _mixer = _bassWrapper.CreateMixerStream(44100, 2, BASSFlag.BASS_MIXER_NONSTOP);
                if (_mixer == 0)
                {
                    _log.LogError("Failed to create mixer: {bassError}", _bassWrapper.GetLastBassError());
                }

                // Start playback of BASS Mixer
                if (!_bassWrapper.Play(_mixer, false))
                {
                    _log.LogError("Failed to play mixer: {bassError}", _bassWrapper.GetLastBassError());
                }

                // Fire the "Player ready" event that tells the playlist handler to start actual playback
                await _cambion.PublishEventAsync(new PlayerReady());

                // Start encoding and streaming to server
                //await _cambion.PublishEventAsync(new StartStreaming());
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Unknown exception during {nameof(BassServiceHost)} startup");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _bassWrapper.StopStreaming();

                // Stop playback of BASS Mixer
                if (!_bassWrapper.Stop(_mixer))
                {
                    _log.LogError("Could not stop mixer: {bassError}", _bassWrapper.GetLastBassError());
                }

                // Uninitialize BASS Mixer
                if (!_bassWrapper.FreeStream(_mixer))
                {
                    _log.LogError("Failed to free mixer: {bassError}", _bassWrapper.GetLastBassError());
                }

                // Uninitialize BASS
                if (!_bassWrapper.Uninitialize())
                {
                    _log.LogError("Could not free BASS.NET resources: {bassError}", _bassWrapper.GetLastBassError());
                }

                UnloadAssemblies();
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Unknown exception during {nameof(BassServiceHost)} shutdown");
            }

            return Task.CompletedTask;
        }

        private void LoadAssemblies()
        {
            var flacLib = "libbassflac.so";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                flacLib = "bassflac.dll";
            }

            DirectoryInfo di = new(Path.Combine(_commonConfig.DataPath, "bass"));

            if (!di.Exists)
            {
                _log.LogCritical("Could not find library path {path}", di.FullName);
            }

            if (_bassWrapper.BassLoadPlugin(Path.Combine(di.FullName, flacLib)) == 0)
            {
                _log.LogCritical("Could not load {flaclib} from {path}", flacLib, di.FullName);
            }

            _log.LogInformation("BASS Version: {bassVersion}", _bassWrapper.GetBassVersion());
            _log.LogInformation("BASS Enc Version: {bassEncVersion}", _bassWrapper.GetBassEncVersion());
            _log.LogInformation("BASS Enc MP3 Version: {bassEncMp3Version}", _bassWrapper.GetBassEncMp3Version());
            _log.LogInformation("BASS Mixer Version: {bassMixerVersion}", _bassWrapper.GetBassMixerVersion());
            _log.LogInformation("BASS.NET Version: {bassMixerVersion}", _bassWrapper.GetBassNetVersion());
        }

        private void UnloadAssemblies()
        {
            _bassWrapper.BassUnloadPlugins();
        }

        public void HandleEvent(PlayTrack input)
        {
            try
            {
                _log.LogTrace("{event} event fired.", nameof(PlayTrack));

                if (input.Track == null)
                {
                    _log.LogError("No track contained in event. Do nothing and let the current track play out.");
                    return;
                }

                // Convert track to extended object
                TrackExt track = new(input.Track);

                // Load music file
                track.ChannelHandle = _bassWrapper.CreateFileStream(track.File, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);

                if (track.ChannelHandle == 0)
                {
                    _log.LogError("Could not create stream from {file}: {bassError}", track.File, _bassWrapper.GetLastBassError());
                    return;
                }

                // Stop previous track (with 1 second fadeout)
                if (_currentlyPlayingTrack != null)
                {
                    _bassWrapper.SlideAttribute(_currentlyPlayingTrack.ChannelHandle, BASSAttribute.BASS_ATTRIB_VOL, -1f, 1000);
                }

                // Add new track to mixer
                if (!_bassWrapper.MixerAddStream(_mixer, track.ChannelHandle, BASSFlag.BASS_MIXER_CHAN_PAUSE | BASSFlag.BASS_MIXER_CHAN_DOWNMIX | BASSFlag.BASS_STREAM_AUTOFREE))
                {
                    _log.LogError("Failed to add channel to mixer: {bassError}", _bassWrapper.GetLastBassError());
                }

                // And start the playback
                if (!_bassWrapper.MixerPlay(track.ChannelHandle))
                {
                    _log.LogError("Failed to play channel: {bassError}", _bassWrapper.GetLastBassError());
                }

                _log.LogDebug("Started playing {file}", track.File);

                // Update the title of the broadcaster
                _bassWrapper.SetStreamingTitle($"{track.Title} - {track.Artist} ({track.Album})");

                // Update which track is currently playing
                _currentlyPlayingTrack = track;
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Unknown error during {nameof(PlayTrack)} event in {nameof(BassServiceHost)}");
            }
        }

        public void HandleEvent(StartStreaming input)
        {
            try
            {
                _bassWrapper.StartStreaming(_mixer);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unknown exceotion during {event}", nameof(StartStreaming));
            }
        }

        public void HandleEvent(StopStreaming input)
        {
            try
            {
                _bassWrapper.StopStreaming();
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unknown exceotion during {event}", nameof(StopStreaming));
            }
        }
    }
}
