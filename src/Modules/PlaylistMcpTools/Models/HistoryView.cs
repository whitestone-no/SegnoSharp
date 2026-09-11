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
/// </summary>
public record HistoryView(
    DateTime ServerTime,
    NowPlaying NowPlaying,
    IReadOnlyList<HistoryEntry> Entries,
    DateTime? ResolvedAt,
    string Hint);