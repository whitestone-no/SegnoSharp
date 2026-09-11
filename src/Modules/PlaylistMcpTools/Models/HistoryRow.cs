using System;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

internal sealed class HistoryRow
{
    public DateTime Played { get; set; }
    public int TrackId { get; set; }
    public int Length { get; set; }
}