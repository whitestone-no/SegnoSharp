using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// Result of an album search.
///
/// <para><see cref="TotalMatches"/> is how many albums matched the filters before the limit
/// was applied, so a caller can tell a complete list from the first page of a longer one.
/// When it exceeds the limit, the returned <see cref="Albums"/> are a slice and must not be
/// described as everything there is.</para>
///
/// <para>For a title search the count reflects everything the title filter matched, which is
/// a superset of the ones that scored well; for a person search it is exact.</para>
///
/// <para><see cref="Truncated"/> means a title search matched more albums than the ranking
/// scan will look at, so the results were chosen from a subset and a better match may exist
/// outside it. A person search never truncates: it orders and slices in the database rather
/// than ranking. Treat it as a signal to search a more specific title, or to add a personId,
/// rather than as a ranked answer.</para>
/// </summary>
public record AlbumSearchResult(
    IReadOnlyList<AlbumResult> Albums,
    int TotalMatches,
    bool Truncated);