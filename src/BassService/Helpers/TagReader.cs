using Whitestone.WASP.BassService.Interfaces;
using Whitestone.WASP.Common.Interfaces;
using Whitestone.WASP.Common.Models;

namespace Whitestone.WASP.BassService.Helpers
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
