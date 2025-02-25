using Whitestone.SegnoSharp.Common.Models;

namespace Whitestone.SegnoSharp.Common.Events
{
    public class StartStreaming(StreamingSettings settings)
    {
        public StreamingSettings Settings { get; set; } = settings;
    }
}
