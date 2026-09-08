using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

public record QueueAddResult(
    IReadOnlyList<int> AddedTrackIds,
    IReadOnlyList<SkippedTrack> Skipped,
    int QueueLength);