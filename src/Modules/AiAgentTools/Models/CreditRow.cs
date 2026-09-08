using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

internal sealed class CreditRow
{
    public string Role { get; set; }
    public List<PersonRow> Persons { get; set; }
}