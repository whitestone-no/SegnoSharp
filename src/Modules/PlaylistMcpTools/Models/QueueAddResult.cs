using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// Outcome of an enqueue.
///
/// <para><see cref="Note"/> carries something the caller should tell the user about the add
/// itself, such as how many tracks it was. It rides back in the response deliberately: an
/// instruction returned alongside the result is acted on far more reliably than the same
/// instruction sitting in a system prompt. Null when there is nothing worth saying.</para>
/// </summary>
public record QueueAddResult(
    IReadOnlyList<int> AddedTrackIds,
    IReadOnlyList<SkippedTrack> Skipped,
    int QueueLength,
    string Note = null);