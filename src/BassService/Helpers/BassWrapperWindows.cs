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

        public bool FreeStream(int handle)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_StreamFree(handle);
        }

        public int AddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user)
        {
            return BassNetWindows::Un4seen.Bass.Bass.BASS_ChannelSetSync(handle, (BassNetWindows::Un4seen.Bass.BASSSync)type, param, new BassNetWindows::Un4seen.Bass.SYNCPROC(proc), user);
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
    }
}
