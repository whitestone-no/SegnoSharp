using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// What is playing and what is coming next.
///
/// <para><see cref="ServerTime"/> is the clock these times are relative to, so a caller with
/// no clock of its own can still reason about "in five minutes".</para>
///
/// <para><see cref="QueueLength"/> is the whole queue; <see cref="Upcoming"/> is the first
/// page of it. Entries on albums the caller may not see are omitted from the page but still
/// counted, so positions match the real queue.</para>
/// </summary>
public record QueueView(
    DateTime ServerTime,
    NowPlaying NowPlaying,
    IReadOnlyList<QueueEntry> Upcoming,
    int QueueLength);