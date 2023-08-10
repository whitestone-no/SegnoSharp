using Whitestone.SegnoSharp.BassService.Interfaces;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;

namespace Whitestone.SegnoSharp.BassService.Helpers
{
    public class TagReader : ITagReader
    {
        private readonly IBassWrapper _bassWrapper;

        public TagReader(IBassWrapper bassWrapper)
        {
            _bassWrapper = bassWrapper;
        }

        public Tags ReadTagInfo(string file)
        {
            return _bassWrapper.GetTagFromFile(file);
        }
    }
}
