namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

public record AlbumTrack(
    byte DiscNumber,
    ushort TrackNumber,
    int TrackId,
    string Title,
    bool IsPlayable);