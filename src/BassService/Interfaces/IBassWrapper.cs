using System;

namespace BassService.Interfaces
{
    public interface IBassWrapper
    {
        void Registration(string email, string key);
        bool Initialize(int device, int frequency, int flags, IntPtr win);
        bool Uninitialize();
        int CreateMixerStream(int frequency, int noOfChannels, int flags);
        bool FreeStream(int handle);
        int AddSynchronizer(int handle, int type, long param, Models.Bass.SYNCPROC proc, IntPtr user);
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
        bool Stop(int handle);
    }
}
