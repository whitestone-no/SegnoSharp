using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

internal sealed class CreditRow
{
    public string Role { get; set; }

    // Whether the role's credits say who a track or album is by, from its stream info.
    public bool IsArtistCredit { get; set; }

    public List<PersonRow> Persons { get; set; }
}