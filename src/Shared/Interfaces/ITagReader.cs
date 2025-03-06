using Whitestone.SegnoSharp.Shared.Models;

namespace Whitestone.SegnoSharp.Shared.Interfaces
{
    public interface ITagReader
    {
        Tags ReadTagInfo(string file);
    }
}
