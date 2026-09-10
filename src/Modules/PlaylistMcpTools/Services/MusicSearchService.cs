using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;
using Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models.Enums;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Helpers;
using Whitestone.SegnoSharp.Shared.Helpers.Security;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Services;

// ReSharper disable EntityFramework.ClientSideDbFunctionCall - These warnings are not really client side as they are inside predicates and are mistakenly interpreted as client side. The predicate itself is used inside a query.

public interface IMusicSearchService
{
    Task<IReadOnlyList<RoleResult>> GetRolesAsync();
    Task<IReadOnlyList<PersonResult>> SearchPeopleAsync(string query, string role = null, int limit = 5, bool allowOnlyPublicAlbums = true);
    Task<AlbumSearchResult> SearchAlbumsAsync(string query = null, int? personId = null, string role = null, int limit = 5, bool allowOnlyPublicAlbums = true);
    Task<TrackSearchResult> SearchTracksAsync(TrackSearchQuery query, double minScore = 0.4, bool allowOnlyPublicAlbums = true);
    Task<AlbumTracklist> GetAlbumTracklistAsync(int albumId, bool allowOnlyPublicAlbums = true);
    Task<QueueAddResult> AddTracksToQueueAsync(IReadOnlyList<int> trackIds, int? position = null, bool playNow = false, bool allowOnlyPublicAlbums = true);
    Task<TrackPickResult> PickTracksAsync(int personId, PickRules rules, DateTime now, Func<int, int> nextRandom, string role = null, int count = 1, bool allowOnlyPublicAlbums = true);
}

/// <summary>
/// Factual read/write tools for a playlist agent. The reads turn fuzzy language
/// into concrete IDs and report how well they matched; the single write acts on IDs.
/// The agent decides whether to ask, act, confirm, or fall back to an external lookup —
/// this class never makes that call, it only surfaces candidates, scores and outcomes.
///
/// "Playlist" here is the single global <see cref="StreamQueue"/>. Unplayable tracks
/// (no <see cref="TrackStreamInfo"/>) are hidden from search; <see cref="GetAlbumTracklistAsync"/>
/// shows everything (marked) so the agent can verify an externally-suggested title.
///
/// Track search is two-stage: the database gathers candidates with a portable LIKE
/// predicate, then ranking happens in memory so no fuzzy logic depends on the SQL provider.
/// Because the candidate gather is capped, it runs in tiers (exact phrase, then all tokens,
/// then any token) and stops at the first tier that returns rows — otherwise a query of
/// common words fills the cap with noise before the scorer ever sees the right track. When
/// the cap is hit anyway, <see cref="TrackSearchResult.Truncated"/> says so.
///
/// minScore is a real filter: candidates below it are never returned. A WeakMatch therefore
/// comes back empty, with TopScore and Hint telling the caller how close it got and what
/// threshold would surface it.
///
/// Query-shape notes for this schema's MySQL/multi-provider setup: everything here is
/// written to translate without a correlated CROSS APPLY (which older MySQL can't run) —
/// i.e. no SelectMany over the many-to-many person link, and no nested collection
/// projection with an inner Take/Distinct/filter. Every row-limiting Take is preceded by
/// a deterministic OrderBy, and the two queries that project more than one collection use
/// AsSplitQuery so EF loads each collection in its own round-trip instead of one query
/// that multiplies rows.
/// </summary>
public class MusicSearchService(
    SegnoSharpDbContext dbContext,
    ICambion cambion) : IMusicSearchService
{
    // Stage-one candidate ceilings, so a vague query ("love") can't drag the whole
    // library into memory. Hitting the track cap is reported as Truncated rather than
    // silently discarding rows the scorer never saw.
    //
    // The track cap is low because a track candidate is fetched with its credits, which is
    // an expensive projection; tiered LIKE predicates keep the 300 rows relevant. Person and
    // album scans project only the columns needed to rank (id, name/title), so they can
    // afford to scan the whole table and fetch the expensive parts afterwards for the
    // winners alone. These two are ceilings against pathological data, not tuning knobs.
    private const int TrackCandidateCap = 300;
    private const int PersonCandidateCap = 5000;
    private const int AlbumTitleScanCap = 5000;

    // Below this score a "best available" candidate is noise rather than a near miss, so the
    // hint stops suggesting a lower threshold and points at the album tracklist instead.
    private const double WeakMatchRetryFloor = 0.3;

    // A role-filtered person search has to compute credit counts before it knows whether a
    // candidate qualifies, so it walks further down the ranked list to fill its limit. This
    // bounds how far.
    private const int RoleFilterScanMultiplier = 3;
    private const int RoleFilterScanFloor = 15;

    public async Task<IReadOnlyList<RoleResult>> GetRolesAsync()
    {
        var raw = await dbContext.PersonGroups
            .Select(pg => new { pg.Name, pg.Type })
            .ToListAsync();

        // The table holds a row per name/scope pair, but callers filter on the name alone,
        // so collapse to one entry per distinct name carrying the scopes it covers.
        return raw
            .GroupBy(pg => pg.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RoleResult(
                g.First().Name,
                g.Select(pg => ToCreditSource(pg.Type)).Distinct().OrderBy(s => s).ToList()))
            .ToList();
    }

    /// <summary>
    /// Resolve a person name to candidates, with per-role credit counts and sample works
    /// so the agent can tell two same-named people apart (the film composer vs the guitarist).
    /// A genuine tie (two distinct people) is the agent's cue to ask.
    ///
    /// When <paramref name="role"/> is supplied it filters the people returned, not just their
    /// counts: a candidate with no credit in that role is dropped, and the scan walks further
    /// down the ranked list to fill <paramref name="limit"/>.
    /// </summary>
    public async Task<IReadOnlyList<PersonResult>> SearchPeopleAsync(
        string query, string role = null, int limit = 5, bool allowOnlyPublicAlbums = true)
    {
        List<string> tokens = TextSearch.Tokenize(query);
        if (tokens.Count == 0)
        {
            return [];
        }

        // Candidate query: any token as a substring of first or last name.
        Expression<Func<Person, bool>> predicate = PredicateBuilder.False<Person>();
        foreach (string token in tokens)
        {
            string pattern = "%" + token + "%";
            predicate = predicate.Or(p =>
                (p.FirstName != null && EF.Functions.Like(p.FirstName.ToLower(), pattern)) ||
                EF.Functions.Like(p.LastName.ToLower(), pattern));
        }

        var raw = await dbContext.Persons
            .Where(predicate)
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.Version })
            .Take(PersonCandidateCap)
            .ToListAsync();

        // Rank in memory against the full name. Without a role filter the top `limit` is all
        // we need; with one, some of them will be discarded, so scan deeper.
        int scanLimit = role == null
            ? limit
            : Math.Max(limit * RoleFilterScanMultiplier, RoleFilterScanFloor);

        var ranked = raw
            .Select(p => new
            {
                p.Id,
                p.Version,
                Name = FormatName(p.FirstName, p.LastName, p.Version),
                TextSearch.ScoreTitle(query, $"{p.FirstName} {p.LastName}").Score
            })
            .OrderByDescending(x => x.Score)
            .Take(scanLimit)
            .ToList();

        if (ranked.Count == 0)
        {
            return [];
        }

        // Credit counts and sample works, one person at a time. The person↔relation
        // link is many-to-many, so flattening it from the Person side (SelectMany over
        // the skip navigation) makes EF emit a correlated CROSS APPLY. Querying from the
        // relation side with a WHERE EXISTS (r.Persons.Any) plus a GROUP BY stays plain
        // JOIN/GROUP BY SQL that translates on every provider. The top set is tiny
        // (<= limit), so the extra round-trips are cheap.
        var results = new List<PersonResult>(Math.Min(ranked.Count, limit));

        foreach (var person in ranked)
        {
            if (results.Count >= limit)
            {
                break;
            }

            int pid = person.Id; // local, so it binds as a query parameter

            IQueryable<TrackPersonGroupPersonRelation> trackRelationQuery = dbContext.TrackPersonGroupsRelations
                .Where(r => (role == null || r.PersonGroup.Name == role)
                            && r.Persons.Any(x => x.Id == pid));

            if (allowOnlyPublicAlbums)
            {
                trackRelationQuery = trackRelationQuery.Where(r => r.Parent.Disc.Album.IsPublic);
            }

            var trackCounts = await trackRelationQuery
                .GroupBy(r => r.PersonGroup.Name)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            IQueryable<AlbumPersonGroupPersonRelation> albumRelationQuery = dbContext.AlbumPersonGroupsRelations
                .Where(r => (role == null || r.PersonGroup.Name == role)
                            && r.Persons.Any(x => x.Id == pid));

            if (allowOnlyPublicAlbums)
            {
                albumRelationQuery = albumRelationQuery.Where(r => r.Parent.IsPublic);
            }

            var albumCounts = await albumRelationQuery
                .GroupBy(r => r.PersonGroup.Name)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            var counts = new Dictionary<string, int>();
            foreach (var c in trackCounts.Concat(albumCounts))
            {
                counts[c.Role] = counts.TryGetValue(c.Role, out int existing) ? existing + c.Count : c.Count;
            }

            // A role filter is a filter on people, not just on their counts: drop candidates
            // who hold no credit in it. Done here rather than in the candidate query because
            // reaching the relations from the Person side would emit a CROSS APPLY.
            if (role != null && (!counts.TryGetValue(role, out int roleCount) || roleCount == 0))
            {
                continue;
            }

            // Sample works: album titles first, topped up with track titles.
            // OrderBy goes after Distinct so the dedup ordering isn't erased.
            IQueryable<AlbumPersonGroupPersonRelation> albumSampleWorksQuery = dbContext.AlbumPersonGroupsRelations
                .Where(r => (role == null || r.PersonGroup.Name == role)
                            && r.Persons.Any(x => x.Id == pid));

            if (allowOnlyPublicAlbums)
            {
                albumSampleWorksQuery = albumSampleWorksQuery.Where(r => r.Parent.IsPublic);
            }

            List<string> samples = await albumSampleWorksQuery
                .Select(r => r.Parent.Title)
                .Distinct()
                .OrderBy(title => title)
                .Take(5)
                .ToListAsync();

            if (samples.Count < 5)
            {
                IQueryable<TrackPersonGroupPersonRelation> trackSampleWorksQuery = dbContext.TrackPersonGroupsRelations
                    .Where(r => (role == null || r.PersonGroup.Name == role)
                                && r.Persons.Any(x => x.Id == pid));

                if (allowOnlyPublicAlbums)
                {
                    trackSampleWorksQuery = trackSampleWorksQuery.Where(r => r.Parent.Disc.Album.IsPublic);
                }

                List<string> trackTitles = await trackSampleWorksQuery
                    .Select(r => r.Parent.Title)
                    .Distinct()
                    .OrderBy(title => title)
                    .Take(5)
                    .ToListAsync();

                samples = samples.Concat(trackTitles).Distinct().Take(5).ToList();
            }

            results.Add(new PersonResult(pid, person.Name, person.Version, counts, samples, Math.Round(person.Score, 3)));
        }

        return results;
    }

    /// <summary>
    /// Resolve albums by title, by credited person, or both, with album-level credits for
    /// disambiguation. At least one of <paramref name="query"/> and <paramref name="personId"/>
    /// is required.
    ///
    /// Person scope unions album-level credits with credits on the album's tracks, matching how
    /// track search scopes by person, so a compilation carrying one track by them still counts.
    /// With no title to rank against, results come back in release order and every hit scores 1.
    ///
    /// TotalMatches reports how many albums matched before the limit, so a caller can tell a
    /// complete list from the first page of a longer one.
    /// </summary>
    public async Task<AlbumSearchResult> SearchAlbumsAsync(
        string query = null, int? personId = null, string role = null, int limit = 5, bool allowOnlyPublicAlbums = true)
    {
        List<string> tokens = string.IsNullOrWhiteSpace(query)
            ? []
            : TextSearch.Tokenize(query);

        bool hasTitle = tokens.Count > 0;

        if (!hasTitle && !personId.HasValue)
        {
            throw new ArgumentException(
                "At least one of query and personId must be supplied. An unfiltered album search returns arbitrary albums.",
                nameof(query));
        }

        IQueryable<Album> albumQuery = dbContext.Albums;

        if (allowOnlyPublicAlbums)
        {
            albumQuery = albumQuery.Where(a => a.IsPublic);
        }

        if (personId.HasValue)
        {
            // Gather the credited album IDs from the relation side, the same way person search
            // does. Reaching the tracks from the Album side would nest four levels of EXISTS
            // (discs -> tracks -> relations -> persons); two flat queries stay readable and
            // translate everywhere.
            int pid = personId.Value;

            List<int> albumCredited = await dbContext.AlbumPersonGroupsRelations
                .Where(r => (role == null || r.PersonGroup.Name == role) && r.Persons.Any(p => p.Id == pid))
                .Select(r => r.Parent.Id)
                .Distinct()
                .ToListAsync();

            List<int> trackCredited = await dbContext.TrackPersonGroupsRelations
                .Where(r => (role == null || r.PersonGroup.Name == role) && r.Persons.Any(p => p.Id == pid))
                .Select(r => r.Parent.Disc.AlbumId)
                .Distinct()
                .ToListAsync();

            List<int> creditedAlbumIds = albumCredited.Union(trackCredited).ToList();

            if (creditedAlbumIds.Count == 0)
            {
                return new AlbumSearchResult([], 0, false);
            }

            albumQuery = albumQuery.Where(a => creditedAlbumIds.Contains(a.Id));
        }

        if (hasTitle)
        {
            Expression<Func<Album, bool>> predicate = PredicateBuilder.False<Album>();
            foreach (string token in tokens)
            {
                string pattern = "%" + token + "%";
                predicate = predicate.Or(a => EF.Functions.Like(a.Title.ToLower(), pattern));
            }

            albumQuery = albumQuery.Where(predicate);
        }

        // Counted before any limit, so the caller can see when it is holding a slice.
        int totalMatches = await albumQuery.CountAsync();

        if (totalMatches == 0)
        {
            return new AlbumSearchResult([], 0, false);
        }

        List<AlbumRow> raw;
        Dictionary<int, double> scores = null;
        bool truncated = false;

        if (hasTitle)
        {
            // Phase one: rank on id and title alone. Fetching credits here would mean pulling
            // a nested collection for every album that shares a word with the query, purely
            // to throw most of them away, which is what forced the old cap to be small enough
            // to cut off real matches.
            var titles = await albumQuery
                .OrderBy(a => a.Id)
                .Select(a => new { a.Id, a.Title })
                .Take(AlbumTitleScanCap + 1)
                .ToListAsync();

            // One row past the ceiling, so hitting it is detectable rather than silent.
            truncated = titles.Count > AlbumTitleScanCap;
            if (truncated)
            {
                titles = titles.Take(AlbumTitleScanCap).ToList();
            }

            scores = titles
                .Select(a => new { a.Id, TextSearch.ScoreTitle(query, a.Title).Score })
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .ToDictionary(x => x.Id, x => x.Score);

            List<int> winners = scores.Keys.ToList();

            // Phase two: the expensive projection, for the handful being returned.
            raw = await dbContext.Albums
                .Where(a => winners.Contains(a.Id))
                .Select(AlbumRowSelector)
                .AsSplitQuery()
                .ToListAsync();
        }
        else
        {
            // Nothing to rank against, so order and slice in the database and fetch the
            // credits for exactly the page being returned.
            raw = await albumQuery
                .OrderBy(a => a.Published).ThenBy(a => a.Title)
                .Select(AlbumRowSelector)
                .Take(limit)
                .AsSplitQuery()
                .ToListAsync();
        }

        IEnumerable<AlbumRow> ordered = hasTitle
            ? raw.OrderByDescending(a => scores[a.Id])
            : raw;

        List<AlbumResult> albums = ordered
            .Select(a => new AlbumResult(
                a.Id,
                a.Title,
                a.Published,
                a.Credits.Select(c => new CreditDto(
                    c.Role,
                    c.Persons.Select(FormatName).ToList(),
                    CreditSource.Album)).ToList(),
                // Nothing to rank against without a title, so every credited album is a full match.
                hasTitle ? Math.Round(scores[a.Id], 3) : 1.0))
            .ToList();

        return new AlbumSearchResult(albums, totalMatches, truncated);
    }

    // ---------- Track search (the crux) ----------

    /// <summary>
    /// Search playable tracks by any combination of title, person/role and album.
    /// Person scope unions direct track credits with credits inherited from the album,
    /// so "by John Williams" still finds tracks credited only at the soundtrack level.
    ///
    /// At least one of TitleQuery, PersonId or AlbumId is required: an unfiltered search
    /// would return arbitrary tracks and — having nothing to rank them against — report
    /// them as a full match.
    ///
    /// <paramref name="minScore"/> filters, it does not merely classify: only candidates at
    /// or above it are returned. The <see cref="TrackSearchResult.Outcome"/> lets the agent
    /// branch — Matched → act; WeakMatch → nothing cleared the bar, but TopScore and Hint say
    /// how close the best one came; NoMatch → nothing exists in this scope at all.
    /// </summary>
    public async Task<TrackSearchResult> SearchTracksAsync(TrackSearchQuery query, double minScore = 0.4, bool allowOnlyPublicAlbums = true)
    {
        List<string> tokens = string.IsNullOrWhiteSpace(query.TitleQuery)
            ? []
            : TextSearch.Tokenize(query.TitleQuery);

        // A title of nothing but punctuation tokenizes to nothing; treat it as no title
        // rather than silently searching the whole scope on its behalf.
        bool hasTitle = tokens.Count > 0;

        if (!hasTitle && !query.PersonId.HasValue && !query.AlbumId.HasValue)
        {
            throw new ArgumentException(
                "At least one of TitleQuery, PersonId or AlbumId must be supplied. An unfiltered track search returns arbitrary tracks.",
                nameof(query));
        }

        IQueryable<Track> scope = dbContext.Tracks.Where(t => t.TrackStreamInfo != null); // playable only

        if (allowOnlyPublicAlbums)
        {
            scope = scope.Where(t => t.Disc.Album.IsPublic);
        }

        if (query.AlbumId.HasValue)
        {
            int albumId = query.AlbumId.Value;
            scope = scope.Where(t => t.Disc.AlbumId == albumId);
        }

        if (query.PersonId.HasValue)
        {
            int personId = query.PersonId.Value;
            string role = query.Role;
            scope = scope.Where(t =>
                t.TrackPersonGroupPersonRelations.Any(r =>
                    (role == null || r.PersonGroup.Name == role) &&
                    r.Persons.Any(p => p.Id == personId))
                ||
                t.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                    (role == null || r.PersonGroup.Name == role) &&
                    r.Persons.Any(p => p.Id == personId)));
        }

        (List<TrackRow> rows, bool truncated) = hasTitle
            ? await GatherTitleCandidatesAsync(scope, query.TitleQuery, tokens)
            : Cap(await TakeCandidatesAsync(scope));

        List<(TrackRow Row, double Score, string MatchedOn)> scored;
        double? topScore;

        if (hasTitle)
        {
            scored = rows
                .Select(r =>
                {
                    (double score, string matchedOn) = TextSearch.ScoreTitle(query.TitleQuery, r.Title);
                    return (Row: r, Score: score, MatchedOn: matchedOn);
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            topScore = scored.Count == 0 ? null : scored[0].Score;

            // minScore is a filter, not a label. Everything below it is dropped, so a
            // WeakMatch hands back an empty list and the agent has to decide out loud
            // whether to retry lower or tell the user nothing good was found.
            scored = scored.Where(x => x.Score >= minScore).ToList();
        }
        else
        {
            // Structural search ("something by X"): no title to rank on, so order for
            // stable output and treat every credited hit as a full match.
            scored = rows
                .OrderBy(r => r.AlbumTitle).ThenBy(r => r.DiscNumber).ThenBy(r => r.TrackNumber)
                .Select(r => (Row: r, Score: 1.0, MatchedOn: "credit match"))
                .ToList();

            topScore = scored.Count == 0 ? null : 1.0;
        }

        SearchOutcome outcome;
        if (rows.Count == 0)
        {
            outcome = SearchOutcome.NoMatch;
        }
        else if (scored.Count > 0)
        {
            outcome = SearchOutcome.Matched;
        }
        else
        {
            outcome = SearchOutcome.WeakMatch;
        }

        List<TrackCandidate> candidates = scored
            .Take(query.Limit)
            .Select(x => ToCandidate(x.Row, x.Score, x.MatchedOn))
            .ToList();

        return new TrackSearchResult(
            outcome,
            candidates,
            topScore.HasValue ? Math.Round(topScore.Value, 3) : null,
            BuildSearchHint(outcome, hasTitle, topScore, minScore, truncated, scored.Count, query.Limit),
            query.AlbumId,
            truncated,
            scored.Count);
    }

    /// <summary>
    /// Gather title candidates in tiers of decreasing precision, stopping at the first tier
    /// that returns anything. The ranking cap is applied to whatever that tier produced, so a
    /// query of common words no longer spends its 300 rows on incidental substring hits.
    /// </summary>
    private async Task<(List<TrackRow> Rows, bool Truncated)> GatherTitleCandidatesAsync(
        IQueryable<Track> scope, string titleQuery, List<string> tokens)
    {
        // Tier 1: the whole query as one contiguous phrase.
        string phrase = TextSearch.NormalizePhrase(titleQuery);
        if (phrase.Length > 0)
        {
            List<TrackRow> phraseRows = await TakeCandidatesAsync(scope.Where(TextSearch.TitlePhraseLike(phrase)));
            if (phraseRows.Count > 0)
            {
                return Cap(phraseRows);
            }
        }

        // Tier 2: every token present, in any order. Only worth a round-trip for multi-token
        // queries — with one token it is the same query as tier 3.
        if (tokens.Count > 1)
        {
            List<TrackRow> allTokenRows = await TakeCandidatesAsync(scope.Where(TextSearch.TitleAllTokensLike(tokens)));
            if (allTokenRows.Count > 0)
            {
                return Cap(allTokenRows);
            }
        }

        // Tier 3: any token. Over-broad by design, and the only tier that survives a typo.
        return Cap(await TakeCandidatesAsync(scope.Where(TextSearch.TitleLike(tokens))));
    }

    // One row past the cap, so hitting it is detectable rather than silent.
    private static Task<List<TrackRow>> TakeCandidatesAsync(IQueryable<Track> scope) =>
        scope
            .OrderBy(t => t.Id)
            .Select(TrackRowSelector)
            .Take(TrackCandidateCap + 1)
            .AsSplitQuery()
            .ToListAsync();

    private static (List<TrackRow> Rows, bool Truncated) Cap(List<TrackRow> rows) =>
        rows.Count > TrackCandidateCap
            ? (rows.Take(TrackCandidateCap).ToList(), true)
            : (rows, false);

    /// <summary>
    /// Plain-language next step for the agent. Scores are formatted with the invariant
    /// culture so a server running under a comma-decimal locale doesn't hand the model a
    /// threshold it can't pass back.
    /// </summary>
    private static string BuildSearchHint(SearchOutcome outcome, bool hasTitle, double? topScore, double minScore, bool truncated, int totalMatches, int limit)
    {
        var parts = new List<string>();

        switch (outcome)
        {
            case SearchOutcome.NoMatch when hasTitle:
                parts.Add("No track title matched in this scope. Resolve the exact track name (e.g. via external lookup) and search again. If the album is known, get all tracks from the album for the real titles.");
                break;

            case SearchOutcome.NoMatch:
                parts.Add("No credited, playable tracks found for this person/role.");
                break;

            case SearchOutcome.WeakMatch:
                double best = topScore ?? 0;
                parts.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "Nothing reached minScore {0:0.##}; the best available scored {1:0.##}, and candidates below minScore are not returned.",
                    minScore,
                    best));

                if (best >= WeakMatchRetryFloor)
                {
                    double retry = Math.Max(WeakMatchRetryFloor, Math.Round(best - 0.05, 2));
                    parts.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "Search once more with minScore {0:0.##} to see it, and tell the user the match is uncertain.",
                        retry));
                }
                else
                {
                    parts.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "That is too low to be meaningful: do not lower minScore below {0:0.##}. Fetch the album tracklist or resolve the exact title externally instead.",
                        WeakMatchRetryFloor));
                }

                break;
        }

        if (totalMatches > limit)
        {
            parts.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Showing the best {0} of {1} matches. Raise limit to see more, and do not describe these as the complete set.",
                limit,
                totalMatches));
        }

        if (truncated)
        {
            parts.Add(string.Format(
                CultureInfo.InvariantCulture,
                "The candidate list was cut off at {0} tracks before ranking, so a better match may exist outside it. Narrow the search with personId or albumId.",
                TrackCandidateCap));
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    /// <summary>
    /// Full disc/track tree for one album, including unplayable tracks (marked), so the
    /// agent can verify an externally-suggested title against what actually exists.
    /// Returns null both when the album does not exist and when it is not visible to this
    /// caller, deliberately: distinguishing them would confirm the existence of private albums.
    /// </summary>
    public async Task<AlbumTracklist> GetAlbumTracklistAsync(int albumId, bool allowOnlyPublicAlbums = true)
    {
        // Album title (and existence check) in one flat query.
        IQueryable<Album> albumQuery = dbContext.Albums
            .Where(a => a.Id == albumId);

        if (allowOnlyPublicAlbums)
        {
            albumQuery = albumQuery.Where(a => a.IsPublic);
        }

        var album = await albumQuery
            .Select(a => new { a.Id, a.Title })
            .FirstOrDefaultAsync();

        if (album == null)
        {
            return null;
        }

        // Query tracks flat from the Tracks root rather than as a nested collection
        // projected off the album. Projecting a.Discs.SelectMany(d => d.Tracks...) inside
        // the album projection makes EF emit a correlated CROSS APPLY (which older MySQL
        // can't run); a flat query over Tracks with a plain JOIN to Disc avoids it.
        IQueryable<Track> trackQuery = dbContext.Tracks
            .Where(t => t.Disc.AlbumId == albumId);

        if (allowOnlyPublicAlbums)
        {
            trackQuery = trackQuery.Where(t => t.Disc.Album.IsPublic);
        }

        List<AlbumTrack> tracks = await trackQuery
            .Select(t => new AlbumTrack(
                t.Disc.DiscNumber,
                t.TrackNumber,
                t.Id,
                t.Title,
                t.TrackStreamInfo != null))
            .ToListAsync();

        List<AlbumTrack> ordered = tracks
            .OrderBy(t => t.DiscNumber)
            .ThenBy(t => t.TrackNumber)
            .ToList();

        return new AlbumTracklist(album.Id, album.Title, ordered);
    }

    // ---------- Pick (weighted, no-repeat "play something by X") ----------

    /// <summary>
    /// Pick <paramref name="count"/> track(s) by a known person, following the same weighting
    /// and no-repeat rules as the auto-playlist — with three deliberate carve-outs for the
    /// "play something by X" case: it ignores <c>IncludeInAutoPlaylist</c> (can pick anything
    /// in the library), skips the artist-repeat rule entirely (we *want* this artist), and
    /// scopes straight to the person instead of computing exclusions across all artists.
    /// Track- and album-repeat rules are still honoured, and within a single multi-pick call
    /// the same album won't be chosen twice while distinct albums remain.
    ///
    /// The volatile collaborators are passed in rather than injected, so this service stays
    /// decoupled from the processor's clock/random/settings and is trivially testable:
    ///   rules      — the repeat windows and weighting flag, from your existing settings;
    ///   now        — from your ISystemClock (systemClock.Now);
    ///   nextRandom — your randomGenerator.GetInt (must return [0, max)).
    ///
    /// PoolSize on the result is the total number of eligible tracks the pick chose from —
    /// the "how many are there really" figure a fixed-size sample can't convey.
    ///
    /// NOTE: the queue-timing and track/album exclusion logic below mirrors
    /// GetNextTrackAsync. If you tune those rules, the two copies will drift — see the note
    /// in chat about extracting a single shared selection core that both call.
    /// </summary>
    public async Task<TrackPickResult> PickTracksAsync(
        int personId,
        PickRules rules,
        DateTime now,
        Func<int, int> nextRandom,
        string role = null,
        int count = 1,
        bool allowOnlyPublicAlbums = true)
    {
        if (count < 1)
        {
            count = 1;
        }

        // Queue end time: summed lengths of everything queued, plus the remainder of whatever
        // is currently playing. Repeat cutoffs are measured from here, because that is when a
        // newly-picked track would actually play.
        List<int> queueLengths = await dbContext.StreamQueue
            .AsNoTracking()
            .Select(q => (int)q.TrackStreamInfo.Track.Length)
            .ToListAsync();
        int queueSum = queueLengths.Sum();

        var currentlyPlaying = await dbContext.StreamHistory
            .AsNoTracking()
            .OrderByDescending(h => h.Played)
            .Select(h => new { h.Played, Length = (int)h.TrackStreamInfo.Track.Length })
            .FirstOrDefaultAsync();

        DateTime currentlyPlayingEnding = now;
        if (currentlyPlaying != null)
        {
            DateTime ending = currentlyPlaying.Played.AddSeconds(currentlyPlaying.Length);
            currentlyPlayingEnding = ending < now ? now : ending;
            double delta = (currentlyPlayingEnding - now).TotalSeconds;
            if (delta > 0)
            {
                queueSum += (int)delta;
            }
        }

        DateTime endOfQueue = now.AddSeconds(queueSum);
        DateTime trackRepeatCutoff = endOfQueue.AddMinutes(-rules.MinutesBetweenTrackRepeat);
        DateTime albumRepeatCutoff = endOfQueue.AddMinutes(-rules.MinutesBetweenAlbumRepeat);

        // Track and album no-repeat exclusions (artist-repeat intentionally NOT computed).
        // Same shape as the auto-playlist: a track/album is excluded if it appears far enough
        // into the queue, or — when the repeat window outlasts the whole queue — if it was
        // played recently per StreamHistory.
        List<int> trackExclusions = await dbContext.TrackStreamInfos
            .AsNoTracking()
            .Where(tsi =>
                tsi.StreamQueue.Any(sq =>
                    currentlyPlayingEnding.AddSeconds(
                        tsi.StreamQueue
                            .Where(q => q.SortOrder <= sq.SortOrder)
                            .Sum(q => q.TrackStreamInfo.Track.Length)
                    ) > trackRepeatCutoff)
                || (rules.MinutesBetweenTrackRepeat * 60 >= queueSum
                    && tsi.StreamHistory.Any(h => h.Played > trackRepeatCutoff)))
            .Select(tsi => tsi.TrackId)
            .Distinct()
            .ToListAsync();

        List<int> albumExclusions = await dbContext.TrackStreamInfos
            .AsNoTracking()
            .Where(tsi =>
                tsi.StreamQueue.Any(sq =>
                    currentlyPlayingEnding.AddSeconds(
                        tsi.StreamQueue
                            .Where(q => q.SortOrder <= sq.SortOrder)
                            .Sum(q => q.TrackStreamInfo.Track.Length)
                    ) > albumRepeatCutoff)
                || (rules.MinutesBetweenAlbumRepeat * 60 >= queueSum
                    && tsi.StreamHistory.Any(h => h.Played > albumRepeatCutoff)))
            .Select(tsi => tsi.Track.Disc.AlbumId)
            .Distinct()
            .ToListAsync();

        // Eligible pool: scoped to the person (track- or album-level credit), minus the
        // track/album exclusions. No IncludeInAutoPlaylist filter, no artist-repeat filter.
        IQueryable<TrackStreamInfo> eligibleQuery = dbContext.TrackStreamInfos
            .AsNoTracking();

        if (allowOnlyPublicAlbums)
        {
            eligibleQuery = eligibleQuery.Where(tsi => tsi.Track.Disc.Album.IsPublic);
        }

        var poolRaw = await eligibleQuery
            .Where(tsi =>
                (tsi.Track.TrackPersonGroupPersonRelations.Any(r =>
                     (role == null || r.PersonGroup.Name == role) && r.Persons.Any(p => p.Id == personId))
                 || tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                     (role == null || r.PersonGroup.Name == role) && r.Persons.Any(p => p.Id == personId)))
                && !trackExclusions.Contains(tsi.TrackId)
                && !albumExclusions.Contains(tsi.Track.Disc.AlbumId))
            .Select(tsi => new { tsi.TrackId, tsi.Track.Disc.AlbumId, tsi.Weight })
            .ToListAsync();

        int poolSize = poolRaw.Count;
        if (poolSize == 0)
        {
            return new TrackPickResult([], 0);
        }

        List<PoolTrack> pool = poolRaw.Select(x => new PoolTrack(x.TrackId, x.AlbumId, x.Weight)).ToList();

        // Weighted (or uniform) sampling without replacement. Prefer an unused album each
        // step; only reuse an album once distinct ones run out, rather than under-filling.
        var chosen = new List<PoolTrack>();
        var remaining = pool.ToList();
        var usedAlbums = new HashSet<int>();
        int want = Math.Min(count, remaining.Count);

        while (chosen.Count < want && remaining.Count > 0)
        {
            List<PoolTrack> candidates = remaining.Where(t => !usedAlbums.Contains(t.AlbumId)).ToList();
            if (candidates.Count == 0)
            {
                candidates = remaining;
            }

            PoolTrack picked = rules.UseWeights
                ? PickWeighted(candidates, nextRandom)
                : PickUniform(candidates, nextRandom);

            chosen.Add(picked);
            usedAlbums.Add(picked.AlbumId);
            remaining.RemoveAll(t => t.TrackId == picked.TrackId);
        }

        // Hydrate the chosen tracks into the same shape search returns (credits, album, etc.),
        // then re-order to the pick order the loop produced.
        List<int> chosenIds = chosen.Select(c => c.TrackId).ToList();

        IQueryable<Track> trackQuery = dbContext.Tracks
            .AsNoTracking();

        if (allowOnlyPublicAlbums)
        {
            trackQuery = trackQuery.Where(t => t.Disc.Album.IsPublic);
        }

        List<TrackRow> rows = await trackQuery
            .Where(t => chosenIds.Contains(t.Id))
            .Select(TrackRowSelector)
            .AsSplitQuery()
            .ToListAsync();

        Dictionary<int, TrackRow> byId = rows.ToDictionary(r => r.Id);

        var picks = new List<TrackCandidate>(chosen.Count);
        foreach (PoolTrack c in chosen)
        {
            if (!byId.TryGetValue(c.TrackId, out TrackRow row))
            {
                continue;
            }

            string how = rules.UseWeights ? $"weighted pick (weight {c.Weight})" : "random pick";
            picks.Add(ToCandidate(row, 0, how));
        }

        return new TrackPickResult(picks, poolSize);
    }

    // ---------- Write ----------

    /// <summary>
    /// Append tracks to the global stream queue (or insert at <paramref name="position"/>,
    /// shifting the rest). Tracks with no playable stream are skipped with a reason rather
    /// than failing the whole call. Returns what actually landed.
    ///
    /// <paramref name="allowOnlyPublicAlbums"/> is enforced here as well as on the reads: IDs
    /// normally come from a filtered search, but the write must not be a way around the
    /// visibility rules for a caller who obtained an ID some other way.
    ///
    /// <paramref name="playNow"/> advances the stream to the next queue entry, which is only
    /// the caller's track if it was inserted at the front — so playNow forces front insertion
    /// and rejects an explicit position elsewhere, rather than quietly playing someone else's
    /// music.
    /// </summary>
    public async Task<QueueAddResult> AddTracksToQueueAsync(
        IReadOnlyList<int> trackIds,
        int? position = null,
        bool playNow = false,
        bool allowOnlyPublicAlbums = true)
    {
        var added = new List<int>();
        var skipped = new List<SkippedTrack>();

        if (playNow)
        {
            if (position is > 0)
            {
                throw new ArgumentException(
                    "playNow advances the stream to the next queue entry, so it only plays the tracks being added when they go to the front. Use position 0, or omit position, together with playNow.",
                    nameof(position));
            }

            position = 0;
        }

        if (trackIds.Count == 0)
        {
            int emptyLen = await dbContext.StreamQueue.CountAsync();
            return new QueueAddResult(added, skipped, emptyLen);
        }

        // One lookup for all requested tracks: TrackStreamInfo is the playability gate, and
        // the album's IsPublic flag is the visibility gate.
        IQueryable<TrackStreamInfo> infoQuery = dbContext.TrackStreamInfos
            .Where(x => trackIds.Contains(x.TrackId));

        if (allowOnlyPublicAlbums)
        {
            infoQuery = infoQuery.Where(x => x.Track.Disc.Album.IsPublic);
        }

        Dictionary<int, TrackStreamInfo> infos = await infoQuery.ToDictionaryAsync(x => x.TrackId);

        int maxSort = await dbContext.StreamQueue.Select(s => (int?)s.SortOrder).MaxAsync() ?? -1;
        int insertAt = position ?? maxSort + 1;
        if (insertAt <= 0)
        {
            insertAt = 1;
        }

        var toAdd = new List<StreamQueue>();
        int cursor = insertAt;

        foreach (int id in trackIds)
        {
            if (!infos.TryGetValue(id, out TrackStreamInfo info))
            {
                // One reason for all three cases, so the response can't be used to probe
                // which private albums exist.
                skipped.Add(new SkippedTrack(id, "Track not found, has no playable stream, or is not available to you."));
                continue;
            }

            if (cursor > ushort.MaxValue)
            {
                skipped.Add(new SkippedTrack(id, "The stream queue is full."));
                continue;
            }

            toAdd.Add(new StreamQueue { TrackStreamInfo = info, SortOrder = (ushort)cursor });
            added.Add(id);
            cursor++;
        }

        // Only shift existing entries when inserting mid-queue.
        if (position is { } pos && toAdd.Count > 0)
        {
            if (pos <= 0)
            {
                pos = 1;
            }

            List<StreamQueue> toShift = await dbContext.StreamQueue
                .Where(s => s.SortOrder >= pos)
                .ToListAsync();

            foreach (StreamQueue s in toShift)
            {
                int shifted = s.SortOrder + toAdd.Count;
                if (shifted > ushort.MaxValue)
                {
                    // Nothing has been saved yet, so bailing out here leaves the queue intact.
                    throw new InvalidOperationException("The stream queue is too long to insert this many tracks at that position.");
                }

                s.SortOrder = (ushort)shifted;
            }
        }

        dbContext.StreamQueue.AddRange(toAdd);
        await dbContext.SaveChangesAsync();

        await cambion.PublishEventAsync(new PlaylistUpdated());

        if (playNow && added.Count > 0)
        {
            await cambion.PublishEventAsync(new PlayNextTrack());
        }

        int queueLength = await dbContext.StreamQueue.CountAsync();
        return new QueueAddResult(added, skipped, queueLength);
    }

    // ---------- Helpers ----------

    private static TrackCandidate ToCandidate(TrackRow r, double score, string matchedOn)
    {
        var credits = new List<CreditDto>();

        foreach (CreditRow c in r.TrackCredits)
        {
            credits.Add(new CreditDto(c.Role, c.Persons.Select(FormatName).ToList(), CreditSource.Track));
        }

        foreach (CreditRow c in r.AlbumCredits)
        {
            credits.Add(new CreditDto(c.Role, c.Persons.Select(FormatName).ToList(), CreditSource.Album));
        }

        return new TrackCandidate(
            r.Id, r.Title, r.TrackNumber, r.DiscNumber, r.AlbumId, r.AlbumTitle,
            r.Year, r.LengthSeconds, credits, Math.Round(score, 3), matchedOn);
    }

    private static string FormatName(PersonRow p) => FormatName(p.First, p.Last, p.Version);

    private static string FormatName(string first, string last, ushort version)
    {
        string name = string.IsNullOrWhiteSpace(first) ? last : $"{first} {last}";
        return version > 0 ? $"{name} ({version})" : name;
    }

    private static PoolTrack PickWeighted(List<PoolTrack> candidates, Func<int, int> nextRandom)
    {
        int weightSum = candidates.Sum(t => t.Weight);
        if (weightSum <= 0)
        {
            return PickUniform(candidates, nextRandom); // all-zero weights: fall back to uniform
        }

        int rnd = nextRandom(weightSum);
        int running = 0;
        foreach (PoolTrack t in candidates)
        {
            running += t.Weight;
            if (running >= rnd)
            {
                return t;
            }
        }

        return candidates[^1]; // rounding safety net
    }

    private static PoolTrack PickUniform(List<PoolTrack> candidates, Func<int, int> nextRandom)
    {
        int index = nextRandom(candidates.Count);
        return candidates.OrderBy(t => t.TrackId).Skip(index).First();
    }

    // Shared album projection, used by both branches of album search so the returned shape
    // (and its split-query behaviour) stays identical whether or not a title was given.
    private static readonly Expression<Func<Album, AlbumRow>> AlbumRowSelector = a => new AlbumRow
    {
        Id = a.Id,
        Title = a.Title,
        Published = a.Published,
        Credits = a.AlbumPersonGroupPersonRelations.Select(r => new CreditRow
        {
            Role = r.PersonGroup.Name,
            Persons = r.Persons.Select(p => new PersonRow
            {
                First = p.FirstName,
                Last = p.LastName,
                Version = p.Version
            }).ToList()
        }).ToList()
    };

    // Shared track projection, used by both SearchTracksAsync and PickTracksAsync's hydrate
    // step so the returned shape (and its APPLY-free / split-query behaviour) stays identical.
    private static readonly Expression<Func<Track, TrackRow>> TrackRowSelector = t => new TrackRow
    {
        Id = t.Id,
        Title = t.Title,
        TrackNumber = t.TrackNumber,
        DiscNumber = t.Disc.DiscNumber,
        AlbumId = t.Disc.AlbumId,
        AlbumTitle = t.Disc.Album.Title,
        Year = t.Disc.Album.Published,
        LengthSeconds = t.Length,
        TrackCredits = t.TrackPersonGroupPersonRelations.Select(r => new CreditRow
        {
            Role = r.PersonGroup.Name,
            Persons = r.Persons.Select(p => new PersonRow
            {
                First = p.FirstName,
                Last = p.LastName,
                Version = p.Version
            }).ToList()
        }).ToList(),
        AlbumCredits = t.Disc.Album.AlbumPersonGroupPersonRelations.Select(r => new CreditRow
        {
            Role = r.PersonGroup.Name,
            Persons = r.Persons.Select(p => new PersonRow
            {
                First = p.FirstName,
                Last = p.LastName,
                Version = p.Version
            }).ToList()
        }).ToList()
    };

    private static CreditSource ToCreditSource(PersonGroupType personGroup)
    {
        return personGroup switch
        {
            PersonGroupType.Album => CreditSource.Album,
            PersonGroupType.Track => CreditSource.Track,
            _ => throw new ArgumentOutOfRangeException(nameof(personGroup), personGroup, null)
        };
    }
}