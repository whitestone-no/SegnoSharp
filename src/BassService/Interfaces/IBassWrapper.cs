using System;

namespace BassService.Interfaces
{
    public interface IBassWrapper
    {
        void Registration(string email, string key);
        bool Initialize(int device, int frequency, int flags, IntPtr win);
        bool BassLoad(string folder);
        bool BassLoadMixer(string folder);
        bool BassLoadFlac(string folder);
        Models.Bass.BASSError GetLastBassError();
        Version GetBassVersion();
        Version GetBassMixerVersion();
    }
}
