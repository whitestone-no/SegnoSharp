extern alias BassNetWindows;

using Whitestone.WASP.BassService.Interfaces;
using System;

namespace Whitestone.WASP.BassService.Helpers
{
    public class BassWrapperWindows : IBassWrapper
    {
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

        public bool BassLoadEncMp3(string folder)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.EncMp3.BassEnc_Mp3.LoadMe(folder);
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

        public bool BassUnloadEncMp3()
        {
            return BassNetWindows::Un4seen.Bass.AddOn.EncMp3.BassEnc_Mp3.FreeMe();
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
    }
}
