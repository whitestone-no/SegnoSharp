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

        public bool BassLoad(string folder)
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.Bass.LoadMe(folder);
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

        public Models.Bass.BASSError GetLastBassError()
        {
            return Enum.TryParse(BassNetLinux::Un4seen.Bass.Bass.BASS_ErrorGetCode().ToString(), out Models.Bass.BASSError outValue) ? outValue : Models.Bass.BASSError.BASS_ERROR_UNKNOWN;
        }

        public Version GetBassVersion()
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_GetVersion(4);
        }

        public Version GetBassMixerVersion()
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_GetVersion(4);
        }
    }
}
