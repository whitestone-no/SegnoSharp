using Whitestone.WASP.Common.Models;

namespace Whitestone.WASP.Common.Interfaces
{
    public interface ITagReader
    {
        Tags ReadTagInfo(string file);
    }
}
