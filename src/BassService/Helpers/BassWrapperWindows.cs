extern alias BassNetWindows;

using Whitestone.WASP.BassService.Interfaces;
using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whitestone.WASP.Common.Models;
using StreamingServer = Whitestone.WASP.Common.Models.Configuration.StreamingServer;

namespace Whitestone.WASP.BassService.Helpers
{
    public class BassWrapperWindows : IBassWrapper
    {
        private readonly ILogger<BassWrapperWindows> _log;
        private readonly StreamingServer _streamingServerConfig;
        private BassNetWindows::Un4seen.Bass.Misc.IBaseEncoder _encoder;
        private BassNetWindows::Un4seen.Bass.Misc.BroadCast _broadCast;
        private BassNetWindows::Un4seen.Bass.Misc.IStreamingServer _streamingServer;
        private string _currentTitle = "WASP";

        public BassWrapperWindows(IOptions<StreamingServer> streamingServerConfig, ILogger<BassWrapperWindows> log)
        {
            _log = log;
            _streamingServerConfig = streamingServerConfig.Value;
        }

        public void Registration(string email, string key)
        {
            BassNetWindows::Un4seen.Bass.BassNet.Registration(email, key);
        }

        public bool Initialize(int device, int frequency, int flags, IntPtr win)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_Init(device, frequency, (BassNetWindows::Un4seen.Bass.BASSInit)flags, win);
        }

        public bool Uninitialize()
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_Free();
        }

        public int CreateMixerStream(int frequency, int noOfChannels, int flags)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamCreate(frequency, noOfChannels, (BassNetWindows::Un4seen.Bass.BASSFlag)flags);
        }

        public int CreateFileStream(string file, long offset, long length, int flags)
        {
            //return BassNetWindows::Un4seen.Bass.Bass.BASS_StreamCreateFile(file, offset, length, (BassNetWindows::Un4seen.Bass.BASSFlag)flags);

            return BassNetWindows::Un4seen.Bass.AddOn.Flac.BassFlac.BASS_FLAC_StreamCreateFile(file, offset, length, (BassNetWindows::Un4seen.Bass.BASSFlag)flags);
        }

        public bool MixerAddStream(int mixerHandle, int streamHandle, int flags)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamAddChannel(mixerHandle, streamHandle, (BassNetWindows::Un4seen.Bass.BASSFlag)flags);
        }

        public bool FreeStream(int handle)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_StreamFree(handle);
        }

        public int AddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_ChannelSetSync(handle, (BassNetWindows::Un4seen.Bass.BASSSync)type, param, new BassNetWindows::Un4seen.Bass.SYNCPROC(proc), user);
        }

        public int MixerAddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelSetSync(handle, (BassNetWindows::Un4seen.Bass.BASSSync)type, param, new BassNetWindows::Un4seen.Bass.SYNCPROC(proc), user);
        }

        public bool MixerRemoveSynchronizer(int handle, int sync)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelRemoveSync(handle, sync);
        }

        public bool BassLoad(string folder)
        {
            return BassNetWindows::Un4seen.Bass.Bass.LoadMe(folder);
        }

        public bool BassLoadEnc(string folder)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Enc.BassEnc.LoadMe(folder);
        }

        public bool BassLoadMixer(string folder)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.LoadMe(folder);
        }

        public bool BassLoadFlac(string folder)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Flac.BassFlac.LoadMe(folder);
        }

        public bool BassUnload()
        {
            return BassNetWindows::Un4seen.Bass.Bass.FreeMe();
        }

        public bool BassUnloadEnc()
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Enc.BassEnc.FreeMe();
        }

        public bool BassUnloadMixer()
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.FreeMe();
        }

        public bool BassUnloadFlac()
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Flac.BassFlac.FreeMe();
        }
        
        public Models.Bass.BASSError GetLastBassError()
        {
            return Enum.TryParse(BassNetWindows::Un4seen.Bass.Bass.BASS_ErrorGetCode().ToString(), out Models.Bass.BASSError outValue) ? outValue : Models.Bass.BASSError.BASS_ERROR_UNKNOWN;
        }

        public Version GetBassVersion()
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_GetVersion(4);
        }

        public Version GetBassEncVersion()
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Enc.BassEnc.BASS_Encode_GetVersion(4);
        }

        public Version GetBassMixerVersion()
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_GetVersion(4);
        }

        public bool Play(int handle, bool restart)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_ChannelPlay(handle, restart);
        }

        public bool MixerPlay(int streamHandle)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelPlay(streamHandle);
        }

        public bool Stop(int handle)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_ChannelStop(handle);
        }

        public bool SlideAttribute(int handle, int attribute, float value, int time)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(handle, (BassNetWindows::Un4seen.Bass.BASSAttribute)attribute, value, time);
        }

        public Tags GetTagFromFile(string file)
        {
            int stream = CreateFileStream(file, 0L, 0L, (int)BassNetWindows::Un4seen.Bass.BASSFlag.BASS_DEFAULT);

            BassNetWindows::Un4seen.Bass.AddOn.Tags.TAG_INFO tags = new BassNetWindows::Un4seen.Bass.AddOn.Tags.TAG_INFO(file);
            if (BassNetWindows::Un4seen.Bass.AddOn.Tags.BassTags.BASS_TAG_GetFromFile(stream, tags))
            {
                return new Tags
                {
                    Album = tags.album,
                    AlbumArtist = tags.albumartist,
                    Artist = tags.artist,
                    Composer = tags.composer,
                    Disc = tags.disc,
                    Duration = tags.duration,
                    Genre = tags.genre,
                    Title = tags.title,
                    TrackNumber = tags.track,
                    Year = tags.year
                };
            }
            else
            {
                _log.LogWarning("Could not read tags from {file}", file);
                return null;
            }
        }

        // This method must be duplicated between Windows and Linux implementations of IBassWrapper
        // because they use interfaces defined in two different DLLs. Therefore implementation
        // can't be shared between the two implementations and they must have their own.
        public void StartStreaming(int channel)
        {
            if (_encoder != null || _streamingServer != null || _broadCast != null)
            {
                return;
            }

            string executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string encoderPath = Path.Combine(executingFolder ?? Directory.GetCurrentDirectory(), "encoder");

            BassNetWindows::Un4seen.Bass.Misc.EncoderCMDLN encoder = new BassNetWindows::Un4seen.Bass.Misc.EncoderCMDLN(channel)
                {
                    EncoderDirectory = encoderPath,
                    CMDLN_Executable = "ffmpeg.exe",
                    CMDLN_CBRString = "-f s16le -ar 44100 -ac 2 -i ${input} -c:a mp3 -b:a ${kbps}k -vn -f mp3 ${output}", // Remember to use "-f adts" for AAC streaming
                CMDLN_EncoderType = BassNetWindows::Un4seen.Bass.BASSChannelType.BASS_CTYPE_STREAM_MP3,
                    CMDLN_DefaultOutputExtension = ".mp3",
                    CMDLN_Bitrate = 320,
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
                _log.LogCritical("Could not find FFMPEG in {0}", encoder.EncoderDirectory);
            }

            BassNetWindows::Un4seen.Bass.Misc.ICEcast icecast = new BassNetWindows::Un4seen.Bass.Misc.ICEcast(_encoder, true)
            {
                ServerAddress = _streamingServerConfig.Address,
                ServerPort = _streamingServerConfig.Port,
                AdminUsername = _streamingServerConfig.AdminUsername,
                AdminPassword = _streamingServerConfig.AdminPassword,
                PublicFlag = _streamingServerConfig.IsPublic,
                MountPoint = _streamingServerConfig.MountPoint,
                Password = _streamingServerConfig.Password,
                StreamGenre = _streamingServerConfig.Genre,
                StreamName = _streamingServerConfig.Name,
                StreamDescription = _streamingServerConfig.Description,
                StreamUrl = _streamingServerConfig.ServerUrl,
                SongTitle = _currentTitle
            };

            _streamingServer = icecast;

            _broadCast = new BassNetWindows::Un4seen.Bass.Misc.BroadCast(_streamingServer)
            {
                AutoReconnect = true,
                ReconnectTimeout = 5
            };
            _broadCast.Notification += BroadCastOnNotification;

            if (!_broadCast.AutoConnect())
            {
                _log.LogError("Could not autoconnect to broadcast server at {address}:{port}{mount}: {lastError} ({lastErrorMessage})", _streamingServerConfig.Address, _streamingServerConfig.Port, _streamingServerConfig.MountPoint, _streamingServer.LastError, _streamingServer.LastErrorMessage);
            }
            else
            {
                _log.LogDebug("Connected to broadcast server at {address}:{port}{mount}", _streamingServerConfig.Address, _streamingServerConfig.Port, _streamingServerConfig.MountPoint);
            }
        }

        // This method must be duplicated between Windows and Linux implementations of IBassWrapper
        // because they use interfaces defined in two different DLLs. Therefore implementation
        // can't be shared between the two implementations and they must have their own.
        public void StopStreaming()
        {
            if (_broadCast != null && _broadCast.IsConnected)
            {
                _broadCast.Disconnect();
                _broadCast = null;
            }

            if (_streamingServer != null && _streamingServer.IsConnected)
            {
                _streamingServer.Disconnect();
                _streamingServer = null;
            }

            if (_encoder != null && _encoder.IsActive)
            {
                if (!_encoder.Stop())
                {
                    _log.LogError("Failed to stop encoder: {0}", GetLastBassError());
                }

                _encoder = null;
            }
        }

        public void SetStreamingTitle(string title)
        {
            _currentTitle = title;

            if (_streamingServer != null)
            {
                if (!_streamingServer.UpdateTitle(title, null))
                {
                    _log.LogWarning("Could not update title on streaming server");
                }
            }
        }

        private void BroadCastOnNotification(object sender, BassNetWindows::Un4seen.Bass.Misc.BroadCastEventArgs e)
        {
            _log.LogDebug("BROADCAST NOTIFICATION: {0}", e.EventType);
        }
    }
}
