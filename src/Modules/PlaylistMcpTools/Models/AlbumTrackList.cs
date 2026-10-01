using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// An album's full tracklist, in disc and track order, including tracks that can't be played.
///
/// <para><see cref="Credits"/> apply to the album as a whole, and so to every track. They are
/// listed once here rather than repeated on each track, where they would add size without
/// adding information. Kept apart, the two can't be confused either: a credit on one track
/// of a compilation is not the album's.</para>
///
/// <para><see cref="Hint"/> says when nobody is credited for the album as a whole, as on most
/// compilations, so an empty list isn't read as an absence to fill in. Null otherwise.</para>
/// </summary>
public record AlbumTracklist(
    int AlbumId,
    string Title,
    IReadOnlyList<CreditDto> Credits,
    IReadOnlyList<AlbumTrack> Tracks,
    string Hint = null);