using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Common.Models.Persistent
{
    public class AudioSettings
    {
        [Persist]
        [DefaultValue(50)]
        public byte Volume { get; set; }
    }
}
