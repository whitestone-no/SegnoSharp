using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// The track the stream is on right now, derived from the newest StreamHistory row: a row is
/// written when a track starts, so <see cref="StartedAt"/> plus the track length is when it
/// ends. Null when the newest row has already finished, which means the stream is idle.
/// All times are server-local wall clock.
///
/// <para>When <see cref="Hidden"/> is true the stream is playing something the caller may not
/// see: the timings are still accurate, but the track, album and credits are withheld. It
/// means "playing something you do not have access to", never "playing nothing".
/// <see cref="Note"/> states this in words, for relaying to the listener; it is null otherwise.</para>
/// </summary>
public record NowPlaying(
    int? TrackId,
    string Title,
    string AlbumTitle,
    IReadOnlyList<CreditDto> Credits,
    DateTime StartedAt,
    int LengthSeconds,
    int ElapsedSeconds,
    int RemainingSeconds,
    bool Hidden,
    string Note);