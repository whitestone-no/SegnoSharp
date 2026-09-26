using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// One album from a search.
///
/// <para><see cref="Note"/> is set when <see cref="PrimaryCredits"/> is empty, saying in words
/// that nobody is credited for the album as a whole. An empty list on its own is an absence,
/// and absences get filled in: a caller looking at a compilation with no album credits tends
/// to promote the artist of whichever track it has in view to artist of the album. The note
/// is true whether the album is a compilation or simply has incomplete data. Null otherwise.</para>
/// </summary>
public record AlbumResult(
    int AlbumId,
    string Title,
    ushort Year,
    IReadOnlyList<CreditDto> PrimaryCredits,
    double Score,
    string Note = null);