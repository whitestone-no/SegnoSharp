using System.Collections.Generic;
using Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models.Enums;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// One credit on a track or album.
///
/// <para><see cref="AppliesTo"/> says what the credit covers: <c>Album</c> means it belongs to
/// the whole album and so to every track on it, <c>Track</c> means it belongs to this one
/// track only. The distinction matters most on compilations, where each track has its own
/// artist and nobody is credited for the album. It was previously named Source, which read as
/// where the data came from rather than what it covers, and callers treated a track's own
/// credit as the album's.</para>
///
/// <para><see cref="IsArtistCredit"/> marks the credits that say who the track or album is
/// by, from the role's own setting. The web page builds its artist line from the same
/// credits — a track's own if it has any, otherwise its album's — so a caller that names the
/// artist the same way says the same as the page. It also
/// decides when two tracks with the same title are by the same artist: an orchestra credited
/// as performer on many scores doesn't count.</para>
/// </summary>
public record CreditDto(
    string Role,
    IReadOnlyList<string> Persons,
    CreditLevel AppliesTo,
    bool IsArtistCredit);