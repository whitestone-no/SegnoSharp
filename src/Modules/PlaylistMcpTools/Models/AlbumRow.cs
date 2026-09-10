using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

internal sealed class AlbumRow
{
    public int Id { get; set; }
    public string Title { get; set; }
    public ushort Published { get; set; }
    public List<CreditRow> Credits { get; set; }
}