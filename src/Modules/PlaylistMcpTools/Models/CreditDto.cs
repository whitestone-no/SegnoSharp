using System.Collections.Generic;
using Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models.Enums;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

public record CreditDto(
    string Role,
    IReadOnlyList<string> Persons,
    CreditSource Source);