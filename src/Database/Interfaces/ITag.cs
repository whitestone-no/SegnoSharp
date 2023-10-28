using System.ComponentModel.DataAnnotations.Schema;

namespace Whitestone.SegnoSharp.Database.Interfaces
{
    public interface ITag
    {
        string TagName { get; set; }

        bool TagEquals(ITag compareObj)
        {
            if (compareObj == null)
            {
                return false;
            }

            return TagName == compareObj.TagName;
        }
    }
}
