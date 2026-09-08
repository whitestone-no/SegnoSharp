using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

internal sealed class TrackRow
{
    public int Id { get; set; }
    public string Title { get; set; }
    public ushort TrackNumber { get; set; }
    public byte DiscNumber { get; set; }
    public int AlbumId { get; set; }
    public string AlbumTitle { get; set; }
    public ushort Year { get; set; }
    public int LengthSeconds { get; set; }
    public List<CreditRow> TrackCredits { get; set; }
    public List<CreditRow> AlbumCredits { get; set; }
}