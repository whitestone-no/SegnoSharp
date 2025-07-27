using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.Misc;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Models.Configuration;
using Whitestone.SegnoSharp.Shared.Models.Persistent;
using Whitestone.SegnoSharp.Modules.BassService.Interfaces;
using Whitestone.SegnoSharp.Modules.BassService.Models;
using Whitestone.SegnoSharp.Modules.BassService.Models.Config;

namespace Whitestone.SegnoSharp.Modules.BassService
{
    public class BassServiceHost : IHostedService,
        IAsyncEventHandler<PlayTrack>,
        IAsyncEventHandler<StartStreaming>,
        IAsyncEventHandler<StopStreaming>,
        IAsyncEventHandler<SetVolume>,
        IAsyncSynchronizedHandler<GetListenersRequest, GetListenersResponse>
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly SiteConfig _siteConfig;
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
            IOptions<SiteConfig> siteConfig,
            ICambion cambion,
            ILogger<BassServiceHost> log,
            StreamingSettings streamingSettings,
            AudioSettings audioSettings)
        {
            _bassWrapper = bassWrapper;
            _siteConfig = siteConfig.Value;
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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await HandleEventAsync(new StopStreaming());

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
        }

        private void LoadAssemblies()
        {
            var flacLib = "libbassflac.so";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                flacLib = "bassflac.dll";
            }

            DirectoryInfo di = new(Path.Combine(_siteConfig.LibPath, "bass"));

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
            _log.LogInformation("BASS Mixer Version: {bassMixerVersion}", _bassWrapper.GetBassMixerVersion());
            _log.LogInformation("BASS.NET Version: {bassMixerVersion}", _bassWrapper.GetBassNetVersion());
        }

        private void UnloadAssemblies()
        {
            _bassWrapper.BassUnloadPlugins();
        }

        public Task HandleEventAsync(PlayTrack input)
        {
            try
            {
                _log.LogTrace("{event} event fired.", nameof(PlayTrack));

                if (input.Track == null)
                {
                    _log.LogError("No track contained in event. Do nothing and let the current track play out.");
                    return Task.CompletedTask;
                }

                // Convert track to extended object
                TrackExt track = new(input.Track);

                // Load music file
                track.ChannelHandle = _bassWrapper.CreateFileStream(track.File, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);

                if (track.ChannelHandle == 0)
                {
                    _log.LogError("Could not create stream from {file}: {bassError}", track.File, _bassWrapper.GetLastBassError());
                    return Task.CompletedTask;
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

            return Task.CompletedTask;
        }

        public Task HandleEventAsync(StartStreaming e)
        {
            try
            {
                if (_encoder != null)
                {
                    return Task.CompletedTask;
                }

                string encoderPath = Path.Combine(_siteConfig.LibPath, _ffmpegConfig.LibFolder);

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

                if (!encoder.EncoderExists)
                {
                    _log.LogCritical("Could not find FFMPEG in {encoderDir}", encoder.EncoderDirectory);
                    return Task.CompletedTask;
                }

                _encoder = encoder;
                _log.LogDebug("BASS Encoder is set up with the following command line: {commandLine}", encoder.EncoderCommandLine);

                if (!encoder.Start(null, IntPtr.Zero, false))
                {
                    _log.LogCritical("Could not start encoder: {bassError}", _bassWrapper.GetLastBassError());
                    return Task.CompletedTask;
                }

                _log.LogDebug("Encoder started");

                string server = _streamingSettings.Hostname + ":" + _streamingSettings.Port;

                if (!string.IsNullOrEmpty(_streamingSettings.MountPoint))
                {
                    server += _streamingSettings.ServerType switch
                    {
                        ServerType.Shoutcast => ",",
                        ServerType.Icecast => "/",
#pragma warning disable CA2208
                        _ => throw new ArgumentOutOfRangeException(nameof(_streamingSettings.ServerType), "Invalid server type")
#pragma warning restore CA2208
                    };

                    server += _streamingSettings.MountPoint.TrimStart('/');
                }

                bool castInitSuccess = _bassWrapper.CastInit(
                    encoder.EncoderHandle,
                    server,
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
                    _log.LogCritical("Could not start casting to {server}. {error}", server, _bassWrapper.GetLastBassError());
                    HandleEventAsync(new StopStreaming()); // Don't await this as we just want to stop the encoder
                    return Task.CompletedTask;
                }

                _log.LogDebug("Casting to {server} started", server);

                UpdateStreamingTitle();

                _streamingSettings.IsStreaming = true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unknown exception during {event}", nameof(StartStreaming));
            }

            return Task.CompletedTask;
        }

        public Task HandleEventAsync(StopStreaming input)
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

            return Task.CompletedTask;
        }

        public Task HandleEventAsync(SetVolume input)
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

            return Task.CompletedTask;
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

        public Task<GetListenersResponse> HandleSynchronizedAsync(GetListenersRequest _)
        {
            //string statsRaw = statsIce;

            BASSEncodeStats type = _streamingSettings.ServerType switch
            {
                ServerType.Icecast => BASSEncodeStats.BASS_ENCODE_STATS_ICESERV,
                ServerType.Shoutcast => BASSEncodeStats.BASS_ENCODE_STATS_SHOUT,
#pragma warning disable CA2208
                _ => throw new ArgumentOutOfRangeException(nameof(_streamingSettings.ServerType), "Invalid server type")
#pragma warning restore CA2208
            };

            string statsRaw = _bassWrapper.GetStreamingStats(_encoder.EncoderHandle, type, null);

            if (statsRaw == null)
            {
                _log.LogError("Failed to get streaming server stats: {error}", _bassWrapper.GetLastBassError());
                return Task.FromResult(new GetListenersResponse());
            }

            XmlDocument statsXml = new();
            statsXml.LoadXml(statsRaw);

            switch (_streamingSettings.ServerType)
            {
                case ServerType.Icecast:
                    return Task.FromResult(GetIcecastListenersFromXml(statsXml));
                case ServerType.Shoutcast:
                    return Task.FromResult(GetShoutcastListenersFromXml(statsXml));
                default:
#pragma warning disable CA2208
                    throw new ArgumentOutOfRangeException(nameof(_streamingSettings.ServerType), "Invalid server type");
#pragma warning restore CA2208
            }
        }

        private GetListenersResponse GetIcecastListenersFromXml(XmlDocument xml)
        {
            GetListenersResponse response = new();

            var sourcePath = $"/icestats/source[@mount='/{_streamingSettings.MountPoint.TrimStart('/')}']";
            XmlNode sourceNode = xml.SelectSingleNode(sourcePath);

            if (sourceNode == null)
            {
                _log.LogError("Could not find {sourcePath} in streaming stats XML", sourcePath);
                return response;
            }

            XmlNode listenersNode = sourceNode.SelectSingleNode("listeners");
            XmlNode peakListenersNode = sourceNode.SelectSingleNode("listener_peak");

            if (listenersNode != null && int.TryParse(listenersNode.InnerText, out int listeners))
            {
                response.Listeners = listeners;
            }

            if (peakListenersNode != null && int.TryParse(peakListenersNode.InnerText, out int peakListeners))
            {
                response.PeakListeners = peakListeners;
            }

            return response;
        }

        private GetListenersResponse GetShoutcastListenersFromXml(XmlDocument xml)
        {
            GetListenersResponse response = new();

            const string sourcePath = $"/SHOUTCASTSERVER";
            XmlNode sourceNode = xml.SelectSingleNode(sourcePath);

            if (sourceNode == null)
            {
                _log.LogError("Could not find {sourcePath} in streaming stats XML", sourcePath);
                return response;
            }

            XmlNode listenersNode = sourceNode.SelectSingleNode("CURRENTLISTENERS");
            XmlNode peakListenersNode = sourceNode.SelectSingleNode("PEAKLISTENERS");

            if (listenersNode != null && int.TryParse(listenersNode.InnerText, out int listeners))
            {
                response.Listeners = listeners;
            }

            if (peakListenersNode != null && int.TryParse(peakListenersNode.InnerText, out int peakListeners))
            {
                response.PeakListeners = peakListeners;
            }

            return response;
        }
    }
}
