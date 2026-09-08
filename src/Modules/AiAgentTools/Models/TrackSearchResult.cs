using System.Collections.Generic;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Models.Enums;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

public record TrackSearchResult(
    SearchOutcome Outcome,
    IReadOnlyList<TrackCandidate> Candidates,
    double? TopScore,
    string Hint,
    int? ScopeAlbumId);