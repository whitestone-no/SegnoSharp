using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass;

namespace Whitestone.SegnoSharp.Modules.TagReader.Interfaces
{
    internal interface IBassWrapper
    {
        void Registration(string email, string key);
        int CreateFileStream(string file, long offset, long length, BASSFlag flags);
        bool GetTagsFromFile(int stream, TAG_INFO tags);
    }
}
