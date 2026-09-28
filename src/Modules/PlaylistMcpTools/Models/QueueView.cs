using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// What is playing and what is coming next.
///
/// <para><see cref="ServerTime"/> is the clock these times are relative to, so a caller with
/// no clock of its own can still reason about "in five minutes".</para>
///
/// <para><see cref="QueueLength"/> is every track waiting behind the one playing now; the
/// playing track lives in the history rather than the queue, so it is never counted.
/// <see cref="Upcoming"/> is the first page of the waiting tracks, numbered from 1 for the
/// one that plays next. Entries on albums the caller may not see are still listed, in position, with
/// their details withheld and Hidden set — so the page is contiguous and the positions are
/// the real ones.</para>
///
/// <para>For a moment ahead, <see cref="Upcoming"/> is centred on the entry expected then
/// rather than starting at the front, and <see cref="TargetTime"/>, <see cref="PrecededBy"/>
/// and <see cref="FollowedBy"/> mirror the history view. <see cref="PrecededBy"/> is null
/// when the match is the next track, since what precedes it is the one playing now.</para>
///
/// <para><see cref="Hint"/> explains when no entry could be matched: the moment is still
/// inside the current track, or it lies past the end of the queue. <see cref="Upcoming"/> is
/// then empty rather than falling back to the front of the queue, which would answer a
/// different question and look like the answer to this one. The second case matters
/// most, because tracks past the end are chosen automatically as the queue plays and genuinely
/// haven't been decided yet — a caller left to work that out tends to name the last entry
/// in the list instead.</para>
/// </summary>
public record QueueView(
    DateTime ServerTime,
    NowPlaying NowPlaying,
    IReadOnlyList<QueueEntry> Upcoming,
    int QueueLength,
    DateTime? TargetTime = null,
    string Hint = null,
    QueueEntry PrecededBy = null,
    QueueEntry FollowedBy = null);