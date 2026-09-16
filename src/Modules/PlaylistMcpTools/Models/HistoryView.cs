using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// What has already played, newest first.
///
/// <para><see cref="ResolvedAt"/> is the point in time the lookup actually used after
/// interpreting the parameters, so a caller can state what it answered rather than what was
/// asked. Null when no point in time was given.</para>
///
/// <para><see cref="Hint"/> explains an empty or surprising result, e.g. no playback recorded
/// on the requested day.</para>
///
/// <para><see cref="PrecededBy"/> and <see cref="FollowedBy"/> are the plays either side of
/// the best match, pulled out by name rather than left to be worked out from the ordering of
/// <see cref="Entries"/>. People misremember times by a few minutes, so these two are usually
/// what they actually meant. <see cref="FollowedBy"/> in particular is easy to overlook,
/// because a question about the past invites looking only backwards. They are chosen before
/// the entry list is trimmed, so asking for a single entry still says what surrounded it —
/// which means either may name a play that is not in <see cref="Entries"/>.</para>
/// </summary>
public record HistoryView(
    DateTime ServerTime,
    NowPlaying NowPlaying,
    IReadOnlyList<HistoryEntry> Entries,
    DateTime? ResolvedAt,
    string Hint,
    HistoryEntry PrecededBy,
    HistoryEntry FollowedBy);