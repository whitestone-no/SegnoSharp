using System;

namespace Whitestone.WASP.BassService.Interfaces
{
    public interface IBassWrapper
    {
        void Registration(string email, string key);
        bool Initialize(int device, int frequency, int flags, IntPtr win);
        bool Uninitialize();
        int CreateMixerStream(int frequency, int noOfChannels, int flags);
        int CreateFileStream(string file, long offset, long length, int flags);
        bool MixerAddStream(int mixerHandle, int streamHandle, int flags);
        bool FreeStream(int handle);
        int AddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user);
        int MixerAddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user);
        bool MixerRemoveSynchronizer(int handle, int sync);
        bool BassLoad(string folder);
        bool BassLoadMixer(string folder);
        bool BassLoadFlac(string folder);
        bool BassLoadEnc(string folder);
        bool BassLoadEncMp3(string folder);
        bool BassUnload();
        bool BassUnloadEnc();
        bool BassUnloadEncMp3();
        bool BassUnloadMixer();
        bool BassUnloadFlac();
        Models.Bass.BASSError GetLastBassError();
        Version GetBassVersion();
        Version GetBassEncVersion();
        Version GetBassMixerVersion();
        bool Play(int handle, bool restart);
        bool MixerPlay(int streamHandle);
        bool Stop(int handle);
        bool SlideAttribute(int handle, int attribute, float value, int time);
    }
}
