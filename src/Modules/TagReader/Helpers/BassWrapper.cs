using Microsoft.Extensions.Logging;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Tags;
using Whitestone.SegnoSharp.Modules.TagReader.Interfaces;

namespace Whitestone.SegnoSharp.Modules.TagReader.Helpers
{
    internal class BassWrapper : IBassWrapper
    {
        public void Registration(string email, string key)
        {
            BassNet.Registration(email, key);
        }

        public int CreateFileStream(string file, long offset, long length, BASSFlag flags)
        {
            return Bass.BASS_StreamCreateFile(file, offset, length, flags);
        }

        public bool GetTagsFromFile(int stream, TAG_INFO tags)
        {
            return BassTags.BASS_TAG_GetFromFile(stream, tags);
        }
    }
}
