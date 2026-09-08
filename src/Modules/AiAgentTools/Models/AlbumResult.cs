using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

public record AlbumResult(
    int AlbumId,
    string Title,
    ushort Year,
    IReadOnlyList<CreditDto> PrimaryCredits,
    double Score);