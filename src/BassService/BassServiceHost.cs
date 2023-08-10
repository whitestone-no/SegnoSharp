extern alias BassNetWindows;
using System;
using Whitestone.SegnoSharp.BassService.Interfaces;
using Whitestone.SegnoSharp.BassService.Models.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
// Make sure to only use enums from BassNetWindows::Un4seen.Bass;
using BassNetWindows::Un4seen.Bass;
using Whitestone.SegnoSharp.Common.Events;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.BassService.Models;

namespace Whitestone.SegnoSharp.BassService
{
    public class BassServiceHost : IHostedService, IEventHandler<PlayTrack>, IEventHandler<StartStreaming>, IEventHandler<StopStreaming>
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly ICambion _cambion;
        private readonly ILogger<BassServiceHost> _log;

        private int _mixer;
        private TrackExt _currentlyPlayingTrack;

        public BassServiceHost(IBassWrapper bassWrapper,
            IOptions<BassRegistration> bassRegistration,
            ICambion cambion,
            ILogger<BassServiceHost> log)
        {
            _bassWrapper = bassWrapper;
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
                if (!_bassWrapper.Initialize(0, 44100, (int)BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
                {
                    _log.LogCritical("Could not initialize BASS.NET: {0}", _bassWrapper.GetLastBassError());
                }

                // Initialize BASS Mixer
                _mixer = _bassWrapper.CreateMixerStream(44100, 2, (int)BASSFlag.BASS_MIXER_NONSTOP);
                if (_mixer == 0)
                {
                    _log.LogError("Failed to create mixer: {0}", _bassWrapper.GetLastBassError());
                }

                // Start playback of BASS Mixer
                if (!_bassWrapper.Play(_mixer, false))
                {
                    _log.LogError("Failed to play mixer: {0}", _bassWrapper.GetLastBassError());
                }

                // Fire the "Player ready" event that tells the playlist handler to start actual playback
                await _cambion.PublishEventAsync(new PlayerReady());

                // Start encoding and streaming to server
                await _cambion.PublishEventAsync(new StartStreaming());
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
                    _log.LogError("Could not stop mixer: {0}", _bassWrapper.GetLastBassError());
                }

                // Uninitialize BASS Mixer
                if (!_bassWrapper.FreeStream(_mixer))
                {
                    _log.LogError("Failed to free mixer: {0}", _bassWrapper.GetLastBassError());
                }

                // Uninitialize BASS
                if (!_bassWrapper.Uninitialize())
                {
                    _log.LogError("Could not free BASS.NET resources: {0}", _bassWrapper.GetLastBassError());
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
            string executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string libraryPath = Path.Combine(executingFolder ?? Directory.GetCurrentDirectory(), "basslibwin");

            if (!_bassWrapper.BassLoad(libraryPath))
            {
                // Don't use GetLastBassError here as Bass hasn't been loaded
                _log.LogCritical("Could not load BASS from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadEnc(libraryPath))
            {
                _log.LogCritical("Could not load bassenc.dll from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadMixer(libraryPath))
            {
                _log.LogCritical("Could not load bassmix.dll from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadFlac(libraryPath))
            {
                _log.LogCritical("Could not load bassflac.dll from {0}", libraryPath);
            }

            _log.LogInformation("BASS Version: {0}", _bassWrapper.GetBassVersion());
            _log.LogInformation("BASS Enc Version: {0}", _bassWrapper.GetBassEncVersion());
            _log.LogInformation("BASS Mixer Version: {0}", _bassWrapper.GetBassMixerVersion());
        }

        private void UnloadAssemblies()
        {
            // Unload the BASS DLLs in the reverse order than they were loaded in.
            // No need to ensure unload with an if as they will always be unloaded when the application ends.
            _bassWrapper.BassUnloadFlac();
            _bassWrapper.BassUnloadMixer();
            _bassWrapper.BassUnloadEnc();
            _bassWrapper.BassUnload();
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
                TrackExt track = new TrackExt(input.Track);

                // Load music file
                track.ChannelHandle = _bassWrapper.CreateFileStream(track.File, 0L, 0L, (int)(BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE));

                if (track.ChannelHandle == 0)
                {
                    _log.LogError("Could not create stream from {0}: {1}", track.File, _bassWrapper.GetLastBassError());
                    return;
                }

                // Stop previous track (with 1 second fadeout)
                if (_currentlyPlayingTrack != null)
                {
                    _bassWrapper.SlideAttribute(_currentlyPlayingTrack.ChannelHandle, (int)BASSAttribute.BASS_ATTRIB_VOL, -1f, 1000);
                }

                // Add new track to mixer
                if (!_bassWrapper.MixerAddStream(_mixer, track.ChannelHandle, (int)(BASSFlag.BASS_MIXER_PAUSE | BASSFlag.BASS_MIXER_DOWNMIX | BASSFlag.BASS_STREAM_AUTOFREE)))
                {
                    _log.LogError("Failed to add channel to mixer: {0}", _bassWrapper.GetLastBassError());
                }

                // And start the playback
                if (!_bassWrapper.MixerPlay(track.ChannelHandle))
                {
                    _log.LogError("Failed to play channel: {0}", _bassWrapper.GetLastBassError());
                }

                _log.LogDebug("Started playing {0}", track.File);

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
