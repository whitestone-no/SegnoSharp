using Whitestone.SegnoSharp.Common.Models.Persistent;

namespace Whitestone.SegnoSharp.Common.Events
{
    public class StartStreaming(StreamingSettings settings)
    {
        public StreamingSettings Settings { get; set; } = settings;
    }
}
