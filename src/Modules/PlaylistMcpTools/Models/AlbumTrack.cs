using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// One track in an album's tracklist.
///
/// <para><see cref="Credits"/> are the track's own credits only. Credits for the album as a
/// whole are listed once on <see cref="AlbumTracklist"/> and apply to every track. On a
/// compilation, which usually has none, these are what say who performs each track.</para>
/// </summary>
public record AlbumTrack(
    byte DiscNumber,
    ushort TrackNumber,
    int TrackId,
    string Title,
    bool IsPlayable,
    IReadOnlyList<CreditDto> Credits);