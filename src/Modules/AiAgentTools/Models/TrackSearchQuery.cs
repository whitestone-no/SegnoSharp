namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

/// <summary>
/// Parameters for a track search. All are optional; combine as needed.
/// TitleQuery drives fuzzy title ranking; PersonId + Role scope by credit
/// (unioning direct track credits with credits inherited from the album);
/// AlbumId scopes to one album.
/// </summary>
public record TrackSearchQuery(
    string TitleQuery = null,
    int? PersonId = null,
    string Role = null,
    int? AlbumId = null,
    int Limit = 10);