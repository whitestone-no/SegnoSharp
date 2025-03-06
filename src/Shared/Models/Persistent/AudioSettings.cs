using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Shared.Models.Persistent
{
    public class AudioSettings
    {
        [Persist]
        [DefaultValue(50)]
        public byte Volume { get; set; }
    }
}
