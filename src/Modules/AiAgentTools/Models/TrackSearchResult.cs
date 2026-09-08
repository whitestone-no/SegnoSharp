using System.Collections.Generic;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Models.Enums;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

/// <summary>
/// Result of a track search.
///
/// <para><see cref="Candidates"/> only ever contains tracks at or above the requested minScore.
/// A <see cref="SearchOutcome.WeakMatch"/> therefore returns an empty list, with
/// <see cref="TopScore"/> reporting how close the best excluded candidate came and
/// <see cref="Hint"/> naming a lower threshold worth retrying.</para>
///
/// <para><see cref="Truncated"/> means the database-side candidate gather hit its ceiling
/// before ranking, so a better match may exist outside the rows that were scored. It is a
/// signal to narrow the search by person or album, not to trust the result as complete.</para>
/// </summary>
public record TrackSearchResult(
    SearchOutcome Outcome,
    IReadOnlyList<TrackCandidate> Candidates,
    double? TopScore,
    string Hint,
    int? ScopeAlbumId,
    bool Truncated);