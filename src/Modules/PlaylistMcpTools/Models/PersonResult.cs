using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

public record PersonResult(
    int PersonId,
    string Name,
    ushort Version,
    IReadOnlyDictionary<string, int> CreditCounts,
    IReadOnlyList<string> SampleWorks,
    double Score);