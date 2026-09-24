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
/// </summary>
public record QueueView(
    DateTime ServerTime,
    NowPlaying NowPlaying,
    IReadOnlyList<QueueEntry> Upcoming,
    int QueueLength);