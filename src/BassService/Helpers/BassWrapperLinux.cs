extern alias BassNetLinux;

using BassService.Interfaces;
using System;

namespace BassService.Helpers
{
    public class BassWrapperLinux : IBassWrapper
    {

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

        public bool FreeStream(int handle)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_StreamFree(handle);
        }

        public int AddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user)
        {

            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelSetSync(handle, (BassNetLinux::Un4seen.Bass.BASSSync)type, param, new BassNetLinux::Un4seen.Bass.SYNCPROC(proc), user);
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

        public bool BassLoadEncMp3(string folder)
        {
            return true;
            //return BassNetWindows::Un4seen.Bass.AddOn.EncMp3.BassEnc_Mp3.LoadMe(folder);
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

        public bool BassUnloadEncMp3()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.EncMp3.BassEnc_Mp3.FreeMe();
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

        public bool Stop(int handle)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelStop(handle);
        }
    }
}
