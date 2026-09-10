using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

public record AlbumTracklist(
    int AlbumId,
    string Title,
    IReadOnlyList<AlbumTrack> Tracks);