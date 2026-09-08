using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

public record TrackPickResult(
    IReadOnlyList<TrackCandidate> Picks,
    int PoolSize);