using Whitestone.SegnoSharp.Common.Models;

namespace Whitestone.SegnoSharp.Common.Interfaces
{
    public interface ITagReader
    {
        Tags ReadTagInfo(string file);
    }
}
