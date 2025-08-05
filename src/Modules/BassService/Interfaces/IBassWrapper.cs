using System;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;

namespace Whitestone.SegnoSharp.Modules.BassService.Interfaces
{
    public interface IBassWrapper
    {
        void Registration(string email, string key);
        bool Initialize(int device, int frequency, BASSInit flags, IntPtr win);
        bool Uninitialize();
        int CreateMixerStream(int frequency, int noOfChannels, BASSFlag flags);
        int CreateFileStream(string file, long offset, long length, BASSFlag flags);
        bool MixerAddStream(int mixerHandle, int streamHandle, BASSFlag flags);
        bool FreeStream(int handle);
        int BassLoadPlugin(string plugin);
        bool BassUnloadPlugins();
        BASSError GetLastBassError();
        Version GetBassVersion();
        Version GetBassEncVersion();
        Version GetBassMixerVersion();
        Version GetBassNetVersion();
        Version GetBassEncMp3Version();
        Version GetBassEncAacVersion();
        bool Play(int handle, bool restart);
        bool MixerPlay(int streamHandle);
        bool Stop(int handle);
        bool SetAttribute(int handle, BASSAttribute attribute, float value);
        bool SlideAttribute(int handle, BASSAttribute attribute, float value, int time);
        int SetSync(int handle, BASSSync type, long param, SYNCPROC proc);
        bool RemoveSync(int handle, int sync);
        bool CastInit(int handle, string server, string pass, string content, string name, string url, string genre, string desc, string headers, int bitrate, BASSEncodeCast flags);
        bool SetStreamingTitle(int handle, string title);
        string GetStreamingStats(int handle, BASSEncodeStats type, string password);
    }
}
