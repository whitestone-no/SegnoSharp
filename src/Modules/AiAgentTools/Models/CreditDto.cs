using System.Collections.Generic;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Models.Enums;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

public record CreditDto(
    string Role,
    IReadOnlyList<string> Persons,
    CreditSource Source);