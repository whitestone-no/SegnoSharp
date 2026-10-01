using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// Random picks for a person, an album, or both.
///
/// <para><see cref="PoolSize"/> is how many eligible tracks the picks were chosen from.</para>
///
/// <para><see cref="Hint"/> says when the repeat rules had to be relaxed to find anything, and
/// which: a pick made that way may have played recently, and a caller that doesn't know will
/// present it as fresh. Relaxing them needs the caller's permission. When <see cref="Picks"/>
/// is empty, the hint says why: nothing matches at all, or — for a caller who may not set the
/// rules aside — everything that matches has played recently. It states what happened rather
/// than offering words to say. Null when the rules held.</para>
/// </summary>
public record TrackPickResult(
    IReadOnlyList<TrackCandidate> Picks,
    int PoolSize,
    string Hint = null);