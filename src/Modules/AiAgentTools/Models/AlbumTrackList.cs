using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

public record AlbumTracklist(
    int AlbumId,
    string Title,
    IReadOnlyList<AlbumTrack> Tracks);