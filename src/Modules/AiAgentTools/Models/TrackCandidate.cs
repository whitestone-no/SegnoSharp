using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

public record TrackCandidate(
    int TrackId,
    string Title,
    ushort TrackNumber,
    byte DiscNumber,
    int AlbumId,
    string AlbumTitle,
    ushort Year,
    int LengthSeconds,
    IReadOnlyList<CreditDto> Credits,
    double Score,
    string MatchedOn);