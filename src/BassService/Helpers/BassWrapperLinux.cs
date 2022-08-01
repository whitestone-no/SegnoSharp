extern alias BassNetLinux;

using Whitestone.WASP.BassService.Interfaces;
using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whitestone.WASP.Common.Models.Configuration;

namespace Whitestone.WASP.BassService.Helpers
{
    public class BassWrapperLinux : IBassWrapper
    {
        private readonly ILogger<BassWrapperWindows> _log;
        private readonly StreamingServer _streamingServerConfig;
        private BassNetLinux::Un4seen.Bass.Misc.IBaseEncoder _encoder;
        private BassNetLinux::Un4seen.Bass.Misc.BroadCast _broadCast;
        private BassNetLinux::Un4seen.Bass.Misc.IStreamingServer _streamingServer;

        public BassWrapperLinux(IOptions<StreamingServer> streamingServerConfig, ILogger<BassWrapperWindows> log)
        {
            _log = log;
            _streamingServerConfig = streamingServerConfig.Value;
        }
        public void Registration(string email, string key)
        {
            BassNetLinux::Un4seen.Bass.BassNet.Registration(email, key);
        }

        public bool Initialize(int device, int frequency, int flags, IntPtr win)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_Init(device, frequency, (BassNetLinux::Un4seen.Bass.BASSInit) flags, win);
        }

        public bool Uninitialize()
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_Free();
        }

        public int CreateMixerStream(int frequency, int noOfChannels, int flags)
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamCreate(frequency, noOfChannels, (BassNetLinux::Un4seen.Bass.BASSFlag)flags);
        }

        public int CreateFileStream(string file, long offset, long length, int flags)
        {
            // return BassNetLinux::Un4seen.Bass.Bass.BASS_StreamCreateFile(file, offset, length, (BassNetLinux::Un4seen.Bass.BASSFlag)flags);

            return BassNetLinux::Un4seen.Bass.AddOn.Flac.BassFlac.BASS_FLAC_StreamCreateFile(file, offset, length, (BassNetLinux::Un4seen.Bass.BASSFlag)flags);
        }

        public bool MixerAddStream(int mixerHandle, int streamHandle, int flags)
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamAddChannel(mixerHandle, streamHandle, (BassNetLinux::Un4seen.Bass.BASSFlag)flags);
        }

        public bool FreeStream(int handle)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_StreamFree(handle);
        }

        public int AddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelSetSync(handle, (BassNetLinux::Un4seen.Bass.BASSSync)type, param, new BassNetLinux::Un4seen.Bass.SYNCPROC(proc), user);
        }

        public int MixerAddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user)
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelSetSync(handle, (BassNetLinux::Un4seen.Bass.BASSSync)type, param, new BassNetLinux::Un4seen.Bass.SYNCPROC(proc), user);
        }

        public bool MixerRemoveSynchronizer(int handle, int sync)
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelRemoveSync(handle, sync);
        }

        public bool BassLoad(string folder)
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.Bass.LoadMe(folder);
        }

        public bool BassLoadEnc(string folder)
        {
            return true;
            //return BassNetWindows::Un4seen.Bass.AddOn.Enc.BassEnc.LoadMe(folder);
        }

        public bool BassLoadMixer(string folder)
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.
        }

        public bool BassLoadFlac(string folder)
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Flac.BassFlac
        }

        public bool BassUnload()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.Bass.FreeMe();
        }

        public bool BassUnloadEnc()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Enc.BassEnc.FreeMe();
        }

        public bool BassUnloadMixer()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.FreeMe();
        }

        public bool BassUnloadFlac()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Flac.BassFlac.FreeMe();
        }

        public Models.Bass.BASSError GetLastBassError()
        {
            return Enum.TryParse(BassNetLinux::Un4seen.Bass.Bass.BASS_ErrorGetCode().ToString(), out Models.Bass.BASSError outValue) ? outValue : Models.Bass.BASSError.BASS_ERROR_UNKNOWN;
        }

        public Version GetBassVersion()
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_GetVersion(4);
        }

        public Version GetBassEncVersion()
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Enc.BassEnc.BASS_Encode_GetVersion(4);
        }

        public Version GetBassMixerVersion()
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_GetVersion(4);
        }

        public bool Play(int handle, bool restart)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelPlay(handle, restart);
        }

        public bool MixerPlay(int streamHandle)
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelPlay(streamHandle);
        }

        public bool Stop(int handle)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelStop(handle);
        }

        public bool SlideAttribute(int handle, int attribute, float value, int time)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(handle, (BassNetLinux::Un4seen.Bass.BASSAttribute)attribute, value, time);
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

            BassNetLinux::Un4seen.Bass.Misc.EncoderCMDLN encoder = new BassNetLinux::Un4seen.Bass.Misc.EncoderCMDLN(channel)
                {
                    EncoderDirectory = encoderPath,
                    CMDLN_Executable = "ffmpeg",
                    CMDLN_CBRString = "-f s16le -ar 44100 -ac 2 -i ${input} -c:a aac -b:a ${kbps}k -vn -f adts ${output}",
                    CMDLN_EncoderType = BassNetLinux::Un4seen.Bass.BASSChannelType.BASS_CTYPE_STREAM_AAC,
                    CMDLN_DefaultOutputExtension = ".aac",
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

            BassNetLinux::Un4seen.Bass.Misc.ICEcast icecast = new BassNetLinux::Un4seen.Bass.Misc.ICEcast(_encoder, true)
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
                StreamDescription = _streamingServerConfig.Description
            };

            _streamingServer = icecast;

            _broadCast = new BassNetLinux::Un4seen.Bass.Misc.BroadCast(_streamingServer)
            {
                AutoReconnect = true,
                ReconnectTimeout = 5
            };
            _broadCast.Notification += BroadCastOnNotification;

            if (!_broadCast.AutoConnect())
            {
                _log.LogError("Could not autoconnect to broadcast server: {0} ({1})", _streamingServer.LastError, _streamingServer.LastErrorMessage);
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

        private void BroadCastOnNotification(object sender, BassNetLinux::Un4seen.Bass.Misc.BroadCastEventArgs e)
        {
            _log.LogDebug("BROADCAST NOTIFICATION: {0}", e.EventType);
        }
    }
}
