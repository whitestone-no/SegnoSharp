using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

public record TrackPickResult(
    IReadOnlyList<TrackCandidate> Picks,
    int PoolSize);