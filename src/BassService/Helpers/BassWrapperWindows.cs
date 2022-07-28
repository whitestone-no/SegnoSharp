extern alias BassNetWindows;

using BassService.Interfaces;
using System;

namespace BassService.Helpers
{
    public class BassWrapperWindows : IBassWrapper
    {
        public void Registration(string email, string key)
        {
            BassNetWindows::Un4seen.Bass.BassNet.Registration(email, key);
        }

        public bool Initialize(int device, int frequency, int flags, IntPtr win)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_Init(device, frequency, (BassNetWindows::Un4seen.Bass.BASSInit) flags, win);
        }

        public bool BassLoad(string folder)
        {
            return BassNetWindows::Un4seen.Bass.Bass.LoadMe(folder);
        }

        public bool BassLoadMixer(string folder)
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.LoadMe(folder);
        }

        public Models.Bass.BASSError GetLastBassError()
        {
            return Enum.TryParse(BassNetWindows::Un4seen.Bass.Bass.BASS_ErrorGetCode().ToString(), out Models.Bass.BASSError outValue) ? outValue : Models.Bass.BASSError.BASS_ERROR_UNKNOWN;
        }

        public Version GetBassVersion()
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_GetVersion(4);
        }

        public Version GetBassMixerVersion()
        {
            return BassNetWindows::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_GetVersion(4);
        }
    }
}
