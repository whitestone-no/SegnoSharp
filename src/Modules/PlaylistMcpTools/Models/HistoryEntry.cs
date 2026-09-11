using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// One play. <see cref="PlayedAt"/> is when it started and <see cref="EndedAt"/> when it
/// finished, both server-local wall clock.
///
/// <para>Entries are normally plays that have already finished; the track still playing is
/// reported separately as nowPlaying. The one exception is a point-in-time lookup that lands
/// inside the current track, where it is the answer and so appears here too. In that case
/// <see cref="StillPlaying"/> is true and <see cref="EndedAt"/> is when it is expected to
/// finish, not when it did.</para>
///
/// <para>When a point in time was asked about, <see cref="BestMatch"/> marks the single entry
/// that best answers it — the one covering most of the requested minute, or the nearest play
/// when nothing was on. Exactly one entry carries it, so there is never a choice to make.</para>
///
/// <para><see cref="OverlapsRequestedTime"/> is broader: it marks every entry that was playing
/// at any point during the requested window. A track boundary often falls inside a minute, so
/// two entries can overlap it while only one is the best match. It saves the caller from
/// having to compare timestamps to work that out.</para>
///
/// <para>When <see cref="Hidden"/> is true the play was on an album the caller may not see.
/// The times and length are still accurate, so the timeline has no unexplained gaps, but the
/// track, album and credits are withheld. <see cref="Note"/> states this in words, for
/// relaying to the listener; it is null otherwise.</para>
/// </summary>
public record HistoryEntry(
    DateTime PlayedAt,
    DateTime EndedAt,
    int? TrackId,
    string Title,
    string AlbumTitle,
    IReadOnlyList<CreditDto> Credits,
    int LengthSeconds,
    bool BestMatch,
    bool OverlapsRequestedTime,
    bool StillPlaying,
    bool Hidden,
    string Note);