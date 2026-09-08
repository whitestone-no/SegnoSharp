namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

public record AlbumTrack(
    byte DiscNumber,
    ushort TrackNumber,
    int TrackId,
    string Title,
    bool IsPlayable);