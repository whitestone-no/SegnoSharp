using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// One upcoming entry. <see cref="Position"/> is 1-based from the front of the queue.
///
/// <para><see cref="EstimatedStart"/> assumes back-to-back playback with no gaps, so it is
/// reliable for the next few entries and drifts further out.</para>
///
/// <para>Nothing records who or what added an entry: automatically selected tracks and ones a
/// listener asked for are indistinguishable here.</para>
///
/// <para>When <see cref="Hidden"/> is true the entry is on an album the caller may not see.
/// It keeps its position, length and timing so the queue stays contiguous and later entries
/// stay correctly placed, but the track, album and credits are withheld. Every position from
/// 1 upwards is present; a hidden entry is a real track, not a gap.
/// <see cref="Note"/> states this in words, for relaying to the listener; it is null otherwise.</para>
/// </summary>
public record QueueEntry(
    int Position,
    int? TrackId,
    string Title,
    string AlbumTitle,
    IReadOnlyList<CreditDto> Credits,
    int LengthSeconds,
    DateTime EstimatedStart,
    bool Hidden,
    string Note);