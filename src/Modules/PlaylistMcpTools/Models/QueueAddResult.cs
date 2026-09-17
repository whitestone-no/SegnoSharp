using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// Outcome of an enqueue.
///
/// <para><see cref="Note"/> carries something the caller should tell the user about the add
/// itself, such as how many tracks it was. It rides back in the response deliberately: an
/// instruction returned alongside the result is acted on far more reliably than the same
/// instruction sitting in a system prompt. Null when there is nothing worth saying.</para>
///
/// <para><see cref="FirstAddedPosition"/> is where the first added track landed, counting from 1
/// at the front of the queue. It exists because a caller that has just queued something
/// naturally wants to say when it will play, and without a real number it will invent one —
/// "up next" for a track sitting fortieth. Null when nothing was added.</para>
/// </summary>
public record QueueAddResult(
    IReadOnlyList<int> AddedTrackIds,
    IReadOnlyList<SkippedTrack> Skipped,
    int QueueLength,
    string Note = null,
    int? FirstAddedPosition = null);