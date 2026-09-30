using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// What has already played, newest first.
///
/// <para><see cref="TargetTime"/> is the point in time the lookup actually used after
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
///
/// <para><see cref="EarlierPlay"/> is set by a track lookup: the play before the flagged one, of
/// any of the tracks asked about, whichever version it was. It is not a neighbour — that is
/// <see cref="PrecededBy"/>, the track that played just before whatever it was — but the
/// previous time the listener heard the song, which may be weeks earlier. It matters most when
/// the flagged play is the one happening now, and "when did we last hear it" means the time
/// before. Null when there is no earlier play, or when the lookup wasn't by track.</para>
/// </summary>
public record HistoryView(
    DateTime ServerTime,
    NowPlaying NowPlaying,
    IReadOnlyList<HistoryEntry> Entries,
    DateTime? TargetTime,
    string Hint,
    HistoryEntry PrecededBy,
    HistoryEntry FollowedBy,
    HistoryEntry EarlierPlay = null);