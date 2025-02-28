using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.Misc;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Common.Events;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Common.Models.Persistent;
using Whitestone.SegnoSharp.Modules.BassService.Interfaces;
using Whitestone.SegnoSharp.Modules.BassService.Models;
using Whitestone.SegnoSharp.Modules.BassService.Models.Config;

namespace Whitestone.SegnoSharp.Modules.BassService
{
    public class BassServiceHost : IHostedService,
        IEventHandler<PlayTrack>,
        IEventHandler<StartStreaming>,
        IEventHandler<StopStreaming>,
        IEventHandler<SetVolume>
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly CommonConfig _commonConfig;
        private readonly Ffmpeg _ffmpegConfig;
        private readonly ICambion _cambion;
        private readonly ILogger<BassServiceHost> _log;
        private readonly StreamingSettings _streamingSettings;
        private readonly AudioSettings _audioSettings;

        private int _mixer;
        private TrackExt _currentlyPlayingTrack;
        private IBaseEncoder _encoder;

        public BassServiceHost(IBassWrapper bassWrapper,
            IOptions<BassRegistration> bassRegistration,
            IOptions<Ffmpeg> ffmpegConfig,
            IOptions<CommonConfig> commonConfig,
            ICambion cambion,
            ILogger<BassServiceHost> log,
            StreamingSettings streamingSettings,
            AudioSettings audioSettings)
        {
            _bassWrapper = bassWrapper;
            _commonConfig = commonConfig.Value;
            _ffmpegConfig = ffmpegConfig.Value;
            _cambion = cambion;
            _log = log;
            _streamingSettings = streamingSettings;
            _audioSettings = audioSettings;

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
                if (_streamingSettings.StartStreamOnStartup)
                {
                    await _cambion.PublishEventAsync(new StartStreaming());
                }
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
                HandleEvent(new StopStreaming());

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
                    _bassWrapper.SlideAttribute(_currentlyPlayingTrack.ChannelHandle, BASSAttribute.BASS_ATTRIB_VOL, 0f, 1000);
                }

                // Set playback volume
                if (!_bassWrapper.SetAttribute(track.ChannelHandle, BASSAttribute.BASS_ATTRIB_VOL, _audioSettings.Volume / 100f))
                {
                    _log.LogError("Could not set volume for {file}: {bassError}", track.File, _bassWrapper.GetLastBassError());
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

                // Update which track is currently playing
                _currentlyPlayingTrack = track;

                // Update the title of the broadcaster
                UpdateStreamingTitle();
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Unknown error during {nameof(PlayTrack)} event in {nameof(BassServiceHost)}");
            }
        }

        public void HandleEvent(StartStreaming e)
        {
            try
            {
                if (_encoder != null)
                {
                    return;
                }

                string encoderPath = Path.Combine(_commonConfig.DataPath, _ffmpegConfig.DataFolder);

                var ffmpegExecutable = "ffmpeg";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ffmpegExecutable = "ffmpeg.exe";
                }

                var format1 = "mp3";
                var format2 = "mp3";
                var extension = ".mp3";
                var encoderCtype = BASSChannelType.BASS_CTYPE_STREAM_MP3;
                string encoderType = BassEnc.BASS_ENCODE_TYPE_MP3;
                if (_streamingSettings.AudioFormat == AudioFormat.Aac)
                {
                    format1 = "aac";
                    format2 = "adts";
                    extension = ".aac";
                    encoderCtype = BASSChannelType.BASS_CTYPE_STREAM_AAC;
                    encoderType = BassEnc.BASS_ENCODE_TYPE_AAC;
                }

                EncoderCMDLN encoder = new(_mixer)
                {
                    EncoderDirectory = encoderPath,
                    CMDLN_Executable = ffmpegExecutable,
                    CMDLN_CBRString = "-f s16le -ar 44100 -ac 2 -i ${input} -c:a " + format1 + " -b:a ${kbps}k -vn -f " + format2 + " ${output}", // Remember to use "-f adts" for AAC streaming
                    CMDLN_EncoderType = encoderCtype,
                    CMDLN_DefaultOutputExtension = extension,
                    CMDLN_Bitrate = (int)_streamingSettings.Bitrate,
                    CMDLN_SupportsSTDOUT = true,
                    CMDLN_ParamSTDIN = "-",
                    CMDLN_ParamSTDOUT = "-"
                };

                if (encoder.EncoderExists)
                {
                    _encoder = encoder;
                    _log.LogDebug("BASS Encoder is set up with the following command line: {commandLine}", encoder.EncoderCommandLine);
                }
                else
                {
                    _log.LogCritical("Could not find FFMPEG in {encoderDir}", encoder.EncoderDirectory);
                }

                if (!encoder.Start(null, IntPtr.Zero, false))
                {
                    _log.LogCritical("Could not start encoder");
                }
                else
                {
                    _log.LogDebug("Encoder started");
                }

                bool castInitSuccess = _bassWrapper.CastInit(
                    encoder.EncoderHandle,
                    _streamingSettings.Hostname + ":" + _streamingSettings.Port + _streamingSettings.MountPoint,
                    _streamingSettings.Password,
                    encoderType,
                    _streamingSettings.Name,
                    _streamingSettings.ServerUrl,
                    _streamingSettings.Genre,
                    _streamingSettings.Description,
                    null,
                    0,
                    _streamingSettings.IsPublic ? BASSEncodeCast.BASS_ENCODE_CAST_PUBLIC : BASSEncodeCast.BASS_ENCODE_CAST_DEFAULT
                );

                if (!castInitSuccess)
                {
                    _log.LogCritical("Could not start casting. {error}", _bassWrapper.GetLastBassError());
                }
                else
                {
                    _log.LogDebug("Casting to {server} started", _streamingSettings.Hostname + ":" + _streamingSettings.Port + _streamingSettings.MountPoint);
                }

                UpdateStreamingTitle();

                _streamingSettings.IsStreaming = true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unknown exception during {event}", nameof(StartStreaming));
            }
        }

        public void HandleEvent(StopStreaming input)
        {
            try
            {
                if (_encoder is { IsActive: true })
                {
                    if (!_encoder.Stop())
                    {
                        _log.LogError("Failed to stop encoder: {error}", _bassWrapper.GetLastBassError());
                    }
                    else
                    {
                        _log.LogDebug("Encoder stopped");
                    }
                }

                _streamingSettings.IsStreaming = false;
                _encoder = null;
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unknown exception during {event}", nameof(StopStreaming));
            }
        }

        public void HandleEvent(SetVolume input)
        {
            try
            {
                _audioSettings.Volume = input.Volume;
                _bassWrapper.SlideAttribute(_currentlyPlayingTrack.ChannelHandle, BASSAttribute.BASS_ATTRIB_VOL, _audioSettings.Volume / 100f, 500);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unknown exception during {event}", nameof(StopStreaming));
            }
        }

        private void UpdateStreamingTitle()
        {
            if (_encoder is not { IsActive: true })
            {
                return;
            }

            var title = "SegnoSharp";

            if (_currentlyPlayingTrack != null)
            {
                title = _streamingSettings.TitleFormat;
                title = title.Replace("%album%", _currentlyPlayingTrack.Album);
                title = title.Replace("%title%", _currentlyPlayingTrack.Title);
                title = title.Replace("%artist%", _currentlyPlayingTrack.Artist);
            }

            if (!_bassWrapper.SetStreamingTitle(_encoder.EncoderHandle, title))
            {
                _log.LogWarning("Could not update title on streaming server. {error}", _bassWrapper.GetLastBassError());
            }
        }
    }
}
