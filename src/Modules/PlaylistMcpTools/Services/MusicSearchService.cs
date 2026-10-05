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
    Task<QueueView> GetQueueAsync(int limit, DateTime now, IReadOnlyList<int> trackIds = null, DateTime? at = null, int atWindowSeconds = 60, bool allowOnlyPublicAlbums = true);
    Task<HistoryView> GetHistoryAsync(int limit, DateTime now, IReadOnlyList<int> trackIds = null, int? personId = null, string role = null, DateTime? at = null, int atWindowSeconds = 60, DateOnly? day = null, bool allowOnlyPublicAlbums = true);
    Task<QueueAddResult> AddTracksToQueueAsync(IReadOnlyList<int> trackIds, PickRules rules, DateTime now, int? position = null, bool playNow = false, bool allowIgnoringRules = false, bool allowOnlyPublicAlbums = true);
    Task<TrackPickResult> PickTracksAsync(PickRules rules, DateTime now, Func<int, int> nextRandom, int? personId = null, string role = null, int? albumId = null, int count = 1, bool allowIgnoringRules = false, bool allowOnlyPublicAlbums = true);
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
/// Candidates are every title sharing a word with the query, ordered so whole-phrase and
/// all-word matches come first — otherwise a query of common words fills the cap with noise
/// before the scorer ever sees the right track. Only id and title are fetched for ranking;
/// credits and album details are loaded afterwards for the page being returned. When the
/// cap is hit anyway, <see cref="TrackSearchResult.Truncated"/> says so.
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
    // Title searches for tracks, people and albums all rank on the columns needed to score
    // (id plus title or name) and fetch the expensive parts — credits, album details —
    // afterwards for the winners alone, so each can afford to scan thousands of candidates.
    // These are ceilings against pathological data, not tuning knobs.
    //
    // A track search with no title needs no ceiling at all: it counts and sorts in the
    // database and loads only the page it returns.
    private const int TrackTitleScanCap = 5000;
    private const int PersonCandidateCap = 5000;
    private const int AlbumTitleScanCap = 5000;

    // Spelled-out explanations for entries the caller may not see. A boolean is easy for a
    // consumer to overlook and quietly omit the entry; a sentence is not.
    private const string HiddenNowPlayingNote =
        "A track on an album you do not have access to. It is playing now, but its details are withheld.";
    private const string HiddenQueueNote =
        "A track on an album you do not have access to. It will play in this position, but its details are withheld.";
    // A reply can take a while to arrive, so a track this close to its end may have finished
    // by the time the listener reads about it.
    private const int NearlyFinishedSeconds = 30;

    // An album's note in search results, where the album is one entry among several, and a
    // tracklist's hint, where the album is the whole response.
    // Searches without a close match point at the other kind before anything else. A title
    // made of ordinary words almost always shares a word with some other title, so a title
    // taken for the wrong kind comes back with loose matches, not with nothing: the pointer
    // has to sit on every outcome without a close match, not only on an empty one. A close
    // match is a result containing every word asked for (TextSearch.ContainsAllWords). The exit
    // clause stops the two searches sending a caller back and forth.
    private const string TryAlbumSearch =
        "The words may name an album rather than a track. If so, playlist_tools__search_albums will find it, and if the words are the album's title, the request is about that album as a whole — its tracks and credits — not about a track of the same name. Skip this if an album search for these words has already found no close match.";

    private const string TryTrackSearch =
        "The words may name a track rather than an album. If so, playlist_tools__search_tracks will find it, and if the words are the track's title, the request is about that track, not about an album of the same name. Skip this if a track search for these words has already found no close match.";

    // A described piece ("the theme from X") has no title in the words to find: rewording the
    // search one variation at a time took ten calls in practice, and picking from memory is a
    // guess. Once a search misses, its real title has to come from outside. But a description
    // can fit several works, and an untargeted web search found the 1980s cartoon's theme when
    // the library held the films' score, so the version is established with an album search
    // first, and named in the web query: one extra call instead of a gamble on the wording.
    private const string DescriptionToWeb =
        "If the words describe a piece rather than name it, as in 'the theme from X', don't try other wordings or pick from memory. A description can fit several works, such as a film and the series it came from, so first establish which one the library holds: unless a search has already shown it, search albums for the work's name alone, X rather than the whole description. Then search the web, with a tool for searching if you have one, using that album name, and optionally the album's release year if there is ambiguity, in the query to find the piece's title, and search for that title here. If no album turns up, search the web without one. If the title the web gives isn't in the library, search the web once more for the version the library holds rather than choosing from memory.";

    private const string NoAlbumMatchHint =
        "No album title matched. " + TryTrackSearch + " " + DescriptionToWeb + " Otherwise check the spelling, or resolve the exact title externally.";

    private const string NoAlbumCreditsNote =
        "Nobody is credited for this album as a whole. Credits on its tracks apply to those tracks only, so don't describe any track's artist as the album's.";
    private const string HiddenHistoryNote =
        "A track on an album you do not have access to. It played at this time, but its details are withheld.";

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

        // The table holds a row per name and type, but callers filter on the name alone, so
        // collapse to one entry per distinct name, with the types it covers as AppliesTo.
        return raw
            .GroupBy(pg => pg.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RoleResult(
                g.First().Name,
                g.Select(pg => ToCreditLevel(pg.Type)).Distinct().OrderBy(s => s).ToList()))
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
    /// With no title to rank against, results come back in title order and every hit scores 1.
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
            // A miss on a title points at the track search before anything else: the words may
            // be a track's title taken for an album's, and an empty answer leaves the web as the
            // obvious next guess.
            return new AlbumSearchResult([], 0, false, hasTitle ? NoAlbumMatchHint : null);
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
                .OrderBy(a => a.Title).ThenBy(a => a.Published).ThenBy(a => a.Id)
                .Select(AlbumRowSelector)
                .Take(limit)
                .AsSplitQuery()
                .ToListAsync();
        }

        IEnumerable<AlbumRow> ordered = hasTitle
            ? raw.OrderByDescending(a => scores[a.Id])
            : raw;

        List<AlbumResult> albums = ordered
            .Select(a =>
            {
                List<CreditDto> credits = a.Credits
                    .Where(c => c.Persons.Count > 0)
                    .Select(c => new CreditDto(c.Role, c.Persons.Select(FormatName).ToList(), CreditLevel.Album))
                    .ToList();

                return new AlbumResult(
                    a.Id,
                    a.Title,
                    a.Published,
                    credits,
                    // Nothing to rank against without a title, so every credited album is a full match.
                    // ReSharper disable once PossibleNullReferenceException - `scores` cannot be null here as it would have failed much earlier.
                    hasTitle ? Math.Round(scores[a.Id], 3) : 1.0,
                    // An empty credit list is an absence, and callers fill it in; say it instead.
                    credits.Count == 0 ? NoAlbumCreditsNote : null);
            })
            .ToList();

        // The count alone gets read past, so say it: a caller handed a page of five tends to
        // treat it as the whole answer.
        string slice = totalMatches > albums.Count
            ? string.Format(
                CultureInfo.InvariantCulture,
                "Showing {0} of {1} matching albums. Raise limit (up to 100) to see more, and don't describe these as the complete set.",
                albums.Count,
                totalMatches)
            : null;

        // Albums that share only some of the words asked for are no answer, and may mean the
        // words were a track's title all along. The same test as the track search: every word
        // present, not a score.
        string loose = hasTitle && !albums.Any(a => TextSearch.ContainsAllWords(query, a.Title))
            ? string.Format(
                CultureInfo.InvariantCulture,
                "None of these albums contains every word asked for: the best, scoring {0:0.##}, shares only some of them. {1} {2} If the words name a title and nothing fits, it is probably not in the library: say so rather than searching further.",
                albums.Count > 0 ? albums.Max(a => a.Score) : 0,
                TryTrackSearch,
                DescriptionToWeb)
            : null;

        string hint = JoinHints(loose, slice);

        return new AlbumSearchResult(albums, totalMatches, truncated, hint);
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
            scope = CreditedTo(scope, query.PersonId.Value, query.Role);
        }

        List<TrackCandidate> candidates;
        double? topScore;
        int totalMatches;
        bool truncated;
        bool anyRows;

        if (hasTitle)
        {
            (List<(int Id, string Title)> titleRows, truncated) = await GatherTitleCandidatesAsync(scope, query.TitleQuery, tokens);
            anyRows = titleRows.Count > 0;

            // Phase one: rank on the title alone. Nothing heavier than id and title has been
            // fetched, which is what lets the scan cover thousands of candidates cheaply.
            var ranked = titleRows
                .Select(r =>
                {
                    (double score, string matchedOn) = TextSearch.ScoreTitle(query.TitleQuery, r.Title);
                    return (r.Id, Score: score, MatchedOn: matchedOn);
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Id)
                .ToList();

            topScore = ranked.Count == 0 ? null : ranked[0].Score;

            // minScore is a filter, not a label. Everything below it is dropped, so a
            // WeakMatch hands back an empty list and the agent has to decide out loud
            // whether to retry lower or tell the user nothing good was found.
            var passing = ranked.Where(x => x.Score >= minScore).ToList();
            totalMatches = passing.Count;

            // Phase two: credits and album details, for the page being returned only.
            var page = passing.Take(query.Limit).ToList();
            Dictionary<int, TrackRow> details = await LoadTrackRowsAsync(page.Select(x => x.Id).ToList(), allowOnlyPublicAlbums);

            candidates = page
                .Where(x => details.ContainsKey(x.Id))
                .Select(x => ToCandidate(details[x.Id], x.Score, x.MatchedOn))
                .ToList();
        }
        else
        {
            // Structural search ("something by X"): no title to rank on. Count exactly and sort
            // in the database, then load details for the page being returned. Nothing is capped,
            // so the total is the real one and the page is the real start of the list — not the
            // start of an arbitrary few hundred, which is what fetching rows in id order and
            // sorting them afterwards used to produce.
            totalMatches = await scope.CountAsync();
            anyRows = totalMatches > 0;
            truncated = false;

            List<int> pageIds = await scope
                .OrderBy(t => t.Disc.Album.Title)
                .ThenBy(t => t.Disc.DiscNumber)
                .ThenBy(t => t.TrackNumber)
                .ThenBy(t => t.Id)
                .Select(t => t.Id)
                .Take(query.Limit)
                .ToListAsync();

            Dictionary<int, TrackRow> details = await LoadTrackRowsAsync(pageIds, allowOnlyPublicAlbums);

            topScore = anyRows ? 1.0 : null;
            candidates = pageIds
                .Where(details.ContainsKey)
                .Select(id => ToCandidate(details[id], 1.0, "credit match"))
                .ToList();
        }

        SearchOutcome outcome;
        if (!anyRows)
        {
            outcome = SearchOutcome.NoMatch;
        }
        else if (totalMatches > 0)
        {
            outcome = SearchOutcome.Matched;
        }
        else
        {
            outcome = SearchOutcome.WeakMatch;
        }

        return new TrackSearchResult(
            outcome,
            candidates,
            topScore.HasValue ? Math.Round(topScore.Value, 3) : null,
            JoinHints(
                BuildSearchHint(outcome, hasTitle, topScore, minScore, truncated, totalMatches, query.Limit, query.AlbumId.HasValue,
                    hasTitle && candidates.Any(c => TextSearch.ContainsAllWords(query.TitleQuery, c.Title))),
                hasTitle && outcome == SearchOutcome.Matched ? SharedTitleHint(candidates) : null),
            query.AlbumId,
            truncated,
            totalMatches);
    }

    /// <summary>
    /// Gather title candidates for ranking: every title sharing at least one word with the
    /// query, strongest first, up to the scan cap.
    ///
    /// The filter is the loosest predicate, so a title sharing only some of the words still
    /// reaches the scorer — "Everything Changes" for "everything about to change" scores well,
    /// but contains neither the phrase nor every word. Stopping at the first stricter predicate
    /// that found anything, as this used to, silently excluded it. The ordering is what keeps
    /// the loose filter from drowning strong matches: whole-phrase titles sort first and
    /// all-words titles next, so they are always inside the cap however many incidental matches
    /// follow. One query, and only id and title, so the cap can be large.
    /// </summary>
    private async Task<(List<(int Id, string Title)> Rows, bool Truncated)> GatherTitleCandidatesAsync(
        IQueryable<Track> scope, string titleQuery, List<string> tokens)
    {
        string phrase = TextSearch.NormalizePhrase(titleQuery);

        var rows = await scope
            .Where(TextSearch.TitleLike(tokens))
            .OrderByDescending(TextSearch.TitleMatchRank(phrase, tokens))
            .ThenBy(t => t.Id)
            .Select(t => new { t.Id, t.Title })
            .Take(TrackTitleScanCap + 1)   // one past the cap, so hitting it is detectable
            .ToListAsync();

        bool truncated = rows.Count > TrackTitleScanCap;

        return (rows.Take(TrackTitleScanCap).Select(r => (r.Id, r.Title)).ToList(), truncated);
    }

    /// <summary>
    /// Plain-language next step for the agent. Scores are formatted with the invariant
    /// culture so a server running under a comma-decimal locale doesn't hand the model a
    /// threshold it can't pass back.
    /// </summary>
    /// <summary>
    /// When a title appears on several albums, say so, with the facts that tell one piece from
    /// several: which albums, how long each copy is, and whether anyone is credited on all of
    /// them. Which album a track came from is the part of an answer callers most often leave
    /// out, and the server can see the overlap where the caller has to notice it. Every title is
    /// checked, not only the best match's: a caller often picks a candidate further down, for
    /// instance when an outside lookup gave it the exact title.
    ///
    /// The server gives no verdict, because it can't know what a piece is. A shared title and a
    /// shared composer are weak evidence: "Main Title" from two films by the same composer is two
    /// pieces, while the same theme on a soundtrack and a compilation is one. The album names
    /// usually settle it, and the caller can read them. When it can't tell, it is told to treat
    /// the copies as different — an unneeded question rather than a wrong track.
    /// </summary>
    private static string SharedTitleHint(IReadOnlyList<TrackCandidate> candidates)
    {
        List<string> notes = candidates
            .Where(c => !string.IsNullOrWhiteSpace(c.Title))
            .GroupBy(c => c.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.ToList())
            .Where(copies => copies.Select(c => c.AlbumId).Distinct().Count() > 1)
            .Take(3)
            .Select(DescribeSharedTitle)
            .ToList();

        return notes.Count == 0 ? null : string.Join(" ", notes);
    }

    private static string DescribeSharedTitle(List<TrackCandidate> copies)
    {
        string title = copies[0].Title.Trim();

        // One copy per album, in ranking order, each with its length: a 5:23 main title and a
        // 2:20 one are plainly different pieces.
        List<TrackCandidate> perAlbum = copies
            .GroupBy(c => c.AlbumId)
            .Select(g => g.First())
            .ToList();

        List<string> shown = perAlbum
            .Take(3)
            .Select(c => string.Format(
                CultureInfo.InvariantCulture,
                "\"{0}\" ({1}:{2:00})",
                c.AlbumTitle,
                c.LengthSeconds / 60,
                c.LengthSeconds % 60))
            .ToList();

        string albums = shown.Count == 1
            ? shown[0]
            : string.Join(", ", shown.Take(shown.Count - 1)) + " and " + shown[^1];

        if (perAlbum.Count > shown.Count)
        {
            albums += string.Format(CultureInfo.InvariantCulture, ", and {0} more", perAlbum.Count - shown.Count);
        }

        // Someone credited on every copy, at each copy's most specific level. One fact among
        // the others, not a verdict.
        var common = new HashSet<string>(MostSpecificPeople(copies[0]), StringComparer.OrdinalIgnoreCase);
        foreach (TrackCandidate copy in copies.Skip(1))
        {
            common.IntersectWith(MostSpecificPeople(copy));
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "\"{0}\" is on {1} albums: {2}, with {3} credited on all of them. If these are one piece, say which album you used and name the others. If they may be different pieces, ask the listener which is meant when playing, with a tool for asking if you have one, and answer for each when asked about one. When unsure, treat them as different.",
            title,
            perAlbum.Count,
            albums,
            common.Count > 0 ? "someone" : "nobody");
    }

    /// <summary>
    /// The people who made a track, at its most specific level: its own credits if it has any,
    /// its album's otherwise. Roles are ignored, since one person can be the Artist on one release
    /// and the Composer on another. Album credits count only when a track has nothing more
    /// specific, so a broad one — "Various Artists" on a compilation, an orchestra across
    /// unrelated pieces — can't make different songs look like the same one.
    /// </summary>
    private static IEnumerable<string> MostSpecificPeople(TrackCandidate track)
    {
        IReadOnlyList<CreditDto> credits = track.Credits ?? [];
        List<CreditDto> own = credits.Where(c => c.AppliesTo == CreditLevel.Track).ToList();

        return (own.Count > 0 ? own : credits.Where(c => c.AppliesTo == CreditLevel.Album))
            .SelectMany(c => c.Persons);
    }

    private static string JoinHints(params string[] hints)
    {
        string joined = string.Join(" ", hints.Where(h => !string.IsNullOrEmpty(h)));
        return joined.Length == 0 ? null : joined;
    }

    private static string BuildSearchHint(SearchOutcome outcome, bool hasTitle, double? topScore, double minScore, bool truncated, int totalMatches, int limit, bool albumScoped, bool closeMatch)
    {
        var parts = new List<string>();

        // Within a known album there's no other kind of title to suggest. A described piece
        // still needs its real title, wherever the search was made.
        string tryAlbums = hasTitle && !albumScoped ? " " + TryAlbumSearch : "";
        string describe = hasTitle ? " " + DescriptionToWeb : "";

        switch (outcome)
        {
            // A Matched result can still be a poor one: in a large library almost any query
            // shares a word with some title. Whether any result has every word asked for is the
            // test, not the score, which counts merely similar words too: one shared word of
            // two scored 0.634 in practice.
            case SearchOutcome.Matched when hasTitle && !closeMatch:
                parts.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "None of these contains every word asked for: the best, scoring {0:0.##}, shares only some of them, so it is probably not the track asked for.{1}{2} If the words name a title and nothing fits, it is probably not in the library: say so rather than searching further.",
                    topScore ?? 0,
                    tryAlbums,
                    describe));
                break;

            case SearchOutcome.NoMatch when hasTitle:
                parts.Add("No track title matched in this scope." + tryAlbums + describe + " Otherwise check the spelling, narrow by person or album, or resolve the exact title externally and search again.");
                break;

            case SearchOutcome.NoMatch:
                parts.Add("No credited, playable tracks found for this person/role.");
                break;

            case SearchOutcome.WeakMatch:
                double best = topScore ?? 0;
                parts.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "Nothing reached minScore {0:0.##}; the best available scored {1:0.##}, and candidates below minScore are not returned.{2}{3}",
                    minScore,
                    best,
                    tryAlbums,
                    describe));

                if (best >= WeakMatchRetryFloor)
                {
                    double retry = Math.Max(WeakMatchRetryFloor, Math.Round(best - 0.05, 2));
                    parts.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "If the words name a title, search once more with minScore {0:0.##} to see it, and tell the user the match is uncertain.",
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
                hasTitle
                    ? "Showing the best {0} of {1} matches. Raise limit to see more, and do not describe these as the complete set."
                    : "Showing the first {0} of {1} matches, in album order. Raise limit to see more, and do not describe these as the complete set.",
                limit,
                totalMatches));
        }

        if (truncated)
        {
            parts.Add("The candidate list was cut off before ranking, so a better match may exist outside it. Narrow the search with personId or albumId.");
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    /// <summary>
    /// Full disc/track tree for one album, including unplayable tracks (marked), so the
    /// agent can verify an externally-suggested title against what actually exists, and with
    /// credits, so it can say who is on the album: the album's own once, each track's on the
    /// track. Returns null both when the album does not exist and when it is not visible to
    /// this caller, deliberately: distinguishing them would confirm the existence of private
    /// albums.
    /// </summary>
    public async Task<AlbumTracklist> GetAlbumTracklistAsync(int albumId, bool allowOnlyPublicAlbums = true)
    {
        // The album, its own credits and the existence check together, through the same
        // projection album search uses, so the credits read the same way in both.
        IQueryable<Album> albumQuery = dbContext.Albums
            .Where(a => a.Id == albumId);

        if (allowOnlyPublicAlbums)
        {
            albumQuery = albumQuery.Where(a => a.IsPublic);
        }

        AlbumRow album = await albumQuery
            .OrderBy(a => a.Id)
            .Select(AlbumRowSelector)
            .AsSplitQuery()
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

        // Each track with its own credits only. The album's apply to every track and are
        // listed once on the tracklist instead: repeated per track they add size without
        // information, and a compilation's per-track credits stay plainly per track.
        var rows = await trackQuery
            .Select(t => new
            {
                t.Disc.DiscNumber,
                t.TrackNumber,
                t.Id,
                t.Title,
                IsPlayable = t.TrackStreamInfo != null,
                Credits = t.TrackPersonGroupPersonRelations.Select(r => new CreditRow
                {
                    Role = r.PersonGroup.Name,
                    Persons = r.Persons.Select(p => new PersonRow
                    {
                        First = p.FirstName,
                        Last = p.LastName,
                        Version = p.Version
                    }).ToList()
                }).ToList()
            })
            .AsSplitQuery()
            .ToListAsync();

        List<AlbumTrack> ordered = rows
            .OrderBy(t => t.DiscNumber)
            .ThenBy(t => t.TrackNumber)
            .Select(t => new AlbumTrack(t.DiscNumber, t.TrackNumber, t.Id, t.Title, t.IsPlayable, ToCredits(t.Credits, CreditLevel.Track)))
            .ToList();

        List<CreditDto> albumCredits = ToCredits(album.Credits, CreditLevel.Album);

        // An empty credit list is an absence, and callers fill it in; say it instead.
        return new AlbumTracklist(album.Id, album.Title, albumCredits, ordered, albumCredits.Count == 0 ? NoAlbumCreditsNote : null);
    }

    // ---------- Pick (weighted, no-repeat "play something by X") ----------

    /// <summary>
    /// Pick <paramref name="count"/> track(s) by a known person, following the same weighting
    /// and no-repeat rules as the auto-playlist — with three deliberate carve-outs for the
    /// "play something by X" case: it ignores <c>IncludeInAutoPlaylist</c> (can pick anything
    /// in the library), skips the artist-repeat rule entirely (we *want* this artist), and
    /// scopes straight to the person instead of computing exclusions across all artists.
    /// Track- and album-repeat rules are still honoured, and set aside one at a time when they
    /// leave nothing to pick only if <paramref name="allowIgnoringRules"/> is true — a permission
    /// of the caller, never a choice of the model. Within a single multi-pick call
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
        PickRules rules,
        DateTime now,
        Func<int, int> nextRandom,
        int? personId = null,
        string role = null,
        int? albumId = null,
        int count = 1,
        bool allowIgnoringRules = false,
        bool allowOnlyPublicAlbums = true)
    {
        if (count < 1)
        {
            count = 1;
        }

        (List<int> trackExclusions, List<int> albumExclusions) = await GetRepeatExclusionsAsync(rules, now);

        // Eligible pool: scoped to the person (track- or album-level credit), minus the
        // track/album exclusions. No IncludeInAutoPlaylist filter, no artist-repeat filter.
        IQueryable<TrackStreamInfo> eligibleQuery = dbContext.TrackStreamInfos
            .AsNoTracking();

        if (allowOnlyPublicAlbums)
        {
            eligibleQuery = eligibleQuery.Where(tsi => tsi.Track.Disc.Album.IsPublic);
        }

        // The pool: tracks credited to the person, on the album, or both. Credits count by the
        // same rule as every other person-scoped lookup.
        IQueryable<TrackStreamInfo> scoped = eligibleQuery;

        if (personId is { } pickPerson)
        {
            IQueryable<int> theirTracks = CreditedTo(dbContext.Tracks, pickPerson, role).Select(t => t.Id);
            scoped = scoped.Where(tsi => theirTracks.Contains(tsi.TrackId));
        }

        if (albumId is { } pickAlbum)
        {
            scoped = scoped.Where(tsi => tsi.Track.Disc.AlbumId == pickAlbum);
        }

        // The repeat rules hold while they leave anything to pick, and are relaxed one at a time
        // when they don't. The album rule goes first because it excludes a whole album at once —
        // for a request naming that album, everything on it. Relaxing only as far as needed means
        // an album heard an hour ago still yields a track not heard for days, rather than
        // yesterday's. The hints say what happened; the caller decides how to put it.
        var tiers = new (bool KeepTrackRule, bool KeepAlbumRule, string Hint)[]
        {
            (true, true, null),
            (true, false, "Nothing was eligible under the repeat rules, so the rule against repeating an album within the hour was set aside."),
            (false, false, "Nothing was eligible even with the album rule set aside, so the rule against repeating a recently played track was set aside too: this has played recently."),
        };

        List<PoolTrack> pool = [];
        string hint = null;

        // Setting the rules aside is a permission, not a default: without it only the first tier
        // runs, and an empty pool stays empty.
        foreach (var tier in allowIgnoringRules ? tiers : tiers[..1])
        {
            IQueryable<TrackStreamInfo> tierQuery = scoped;

            if (tier.KeepTrackRule)
            {
                tierQuery = tierQuery.Where(tsi => !trackExclusions.Contains(tsi.TrackId));
            }

            if (tier.KeepAlbumRule)
            {
                tierQuery = tierQuery.Where(tsi => !albumExclusions.Contains(tsi.Track.Disc.AlbumId));
            }

            var found = await tierQuery
                .Select(tsi => new { tsi.TrackId, tsi.Track.Disc.AlbumId, tsi.Weight })
                .ToListAsync();

            if (found.Count > 0)
            {
                pool = found.Select(x => new PoolTrack(x.TrackId, x.AlbumId, x.Weight)).ToList();
                hint = tier.Hint;
                break;
            }
        }

        int poolSize = pool.Count;
        if (poolSize == 0)
        {
            if (allowIgnoringRules)
            {
                // Every rule relaxed and still nothing: there is genuinely nothing playable here.
                return new TrackPickResult([], 0, "Nothing playable matches, even with the repeat rules set aside.");
            }

            // Without the permission, empty has two causes, and they mean opposite things to a
            // listener: nothing by them at all, or plenty that has all played recently. Reported
            // as one, the second reads as the first.
            return await scoped.AnyAsync()
                ? new TrackPickResult([], 0, "Everything that matches has played recently, so the repeat rules leave nothing to pick. They apply however a track is chosen, so tell the listener rather than looking for one another way.")
                : new TrackPickResult([], 0, "Nothing playable matches.");
        }

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

        return new TrackPickResult(picks, poolSize, hint);
    }

    // ---------- Stream state (read-only) ----------

    /// <summary>
    /// What is playing and what is queued behind it.
    ///
    /// The queue is always kept populated, partly by listeners and partly by the auto-playlist,
    /// and nothing records which is which — so this reports what will play, never who asked
    /// for it.
    ///
    /// <paramref name="now"/> comes from the caller's clock rather than DateTime.Now so the
    /// comparison against StreamHistory.Played stays testable. Both are server-local wall
    /// clock with an unspecified Kind; if Played ever moves to UTC, convert at the tool
    /// boundary rather than here.
    /// </summary>
    public async Task<QueueView> GetQueueAsync(
        int limit, DateTime now, IReadOnlyList<int> trackIds = null, DateTime? at = null, int atWindowSeconds = 60, bool allowOnlyPublicAlbums = true)
    {
        limit = Math.Clamp(limit, 1, 100);
        atWindowSeconds = Math.Clamp(atWindowSeconds, 1, 3600);

        NowPlaying nowPlaying = await GetNowPlayingAsync(now, allowOnlyPublicAlbums);

        // The whole queue in order, but only the two columns the timing needs. That stays cheap
        // for a long queue, and means every estimated start is computed from the front whichever
        // page is returned — so a page centred an hour ahead is timed correctly too.
        var order = await dbContext.StreamQueue
            .AsNoTracking()
            .OrderBy(q => q.SortOrder)
            .Select(q => new
            {
                q.TrackStreamInfo.TrackId,
                Length = (int)q.TrackStreamInfo.Track.Length
            })
            .ToListAsync();

        // Estimated starts run from the end of the current track, then accumulate. Expressing
        // them as HistoryRows lets a moment ahead be matched by exactly the rule the history
        // uses for a moment past.
        DateTime firstStart = nowPlaying == null ? now : now.AddSeconds(nowPlaying.RemainingSeconds);
        var timeline = new List<HistoryRow>(order.Count);
        DateTime cursor = firstStart;
        foreach (var row in order)
        {
            timeline.Add(new HistoryRow { Played = cursor, TrackId = row.TrackId, Length = row.Length });
            cursor = cursor.AddSeconds(row.Length);
        }

        DateTime queueEnds = cursor;

        int matchIndex = -1;
        string hint = null;

        // The requested minute, as one value. Start and end are set together or not at all,
        // so nothing downstream has to infer that one being present means the other is.
        (DateTime Start, DateTime End)? window = null;

        if (trackIds is { Count: > 0 })
        {
            // Answer "where is this track" directly. Left to page through the queue, a caller
            // that can't find a track it remembers queueing concludes the response is wrong.
            // Matching by ID rather than position also finds a track someone has moved: paging
            // would miss one moved past the end of the page and take it for removed. Several IDs
            // — an album, or every version of a song — flag whichever of them comes first.
            var ids = trackIds.ToHashSet();
            bool single = ids.Count == 1;

            List<int> positions = timeline
                .Select((row, i) => (row.TrackId, i))
                .Where(x => ids.Contains(x.TrackId))
                .Select(x => x.i)
                .ToList();

            bool onePlayingNow = nowPlaying?.TrackId is { } playingId && ids.Contains(playingId);

            if (positions.Count > 0)
            {
                matchIndex = positions[0];

                var notes = new List<string>();
                if (onePlayingNow)
                {
                    // Mid-album, "when will it play" is best answered "it already has".
                    notes.Add(single ? "That track is also playing now." : "One of those tracks is playing now.");
                }

                if (positions.Count > 1)
                {
                    notes.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} queued entries match; the first is flagged.",
                        positions.Count));
                }

                hint = notes.Count == 0 ? null : string.Join(" ", notes);
            }
            else if (onePlayingNow)
            {
                hint = single
                    ? "That track is playing now, which is reported as nowPlaying."
                    : "One of those tracks is playing now, which is reported as nowPlaying. None of the others is queued.";
            }
            else
            {
                // Gone is not the same as removed: a track queued a while ago may simply have
                // played. Say both, so the caller doesn't assert the wrong one.
                hint = single
                    ? "That track isn't in the queue. It has either been removed or already played; the history shows which. Don't queue it again unless the listener asks you to."
                    : "None of those tracks are in the queue. They have either been removed or already played; the history shows which. Don't queue them again unless the listener asks you to.";
            }
        }
        else if (at is { } point)
        {
            window = (point, point.AddSeconds(atWindowSeconds));

            if (nowPlaying != null && point < firstStart)
            {
                hint = "That moment falls within the track playing now, which is reported as nowPlaying.";
            }
            else if (point >= queueEnds)
            {
                // Past the end of the queue nothing has been chosen yet: the auto-playlist
                // fills it as it plays. Saying so matters, because otherwise the last entry in
                // the list looks like the answer.
                hint = string.Format(
                    CultureInfo.InvariantCulture,
                    "The queue runs out at about {0}. Tracks after that are chosen automatically as it plays, so what will be on at {1} hasn't been decided yet.",
                    DescribeMoment(queueEnds, now),
                    DescribeMoment(point, now));
            }
            else
            {
                HistoryRow anchor = ChooseAnchor(timeline, point, atWindowSeconds, out _);
                matchIndex = anchor == null ? -1 : timeline.IndexOf(anchor);
            }
        }

        // The page: the front of the queue normally, or centred on the match for a lookup.
        // A lookup with no match gets no page at all. The front of the queue would answer a
        // different question — what's next — and beside a hint saying nothing has been chosen
        // yet, any entry listed reads as the answer.
        int first = 0;
        int last = Math.Min(limit, timeline.Count);

        bool lookup = window.HasValue || trackIds is { Count: > 0 };

        if (lookup && matchIndex < 0)
        {
            last = 0;
        }
        else if (matchIndex >= 0 && timeline.Count > limit)
        {
            first = Math.Clamp(matchIndex - ((limit - 1) / 2), 0, timeline.Count - limit);
            last = first + limit;
        }

        // Details for the page, plus the neighbours, which can fall just outside it.
        var needed = new List<int>();
        for (int i = first; i < last; i++)
        {
            needed.Add(timeline[i].TrackId);
        }

        if (matchIndex > 0)
        {
            needed.Add(timeline[matchIndex - 1].TrackId);
        }

        if (matchIndex >= 0 && matchIndex + 1 < timeline.Count)
        {
            needed.Add(timeline[matchIndex + 1].TrackId);
        }

        Dictionary<int, TrackRow> tracks = await LoadTrackRowsAsync(needed.Distinct().ToList(), allowOnlyPublicAlbums);

        var upcoming = new List<QueueEntry>(last - first);
        for (int i = first; i < last; i++)
        {
            upcoming.Add(Build(i));
        }

        // The track before the next one is the one playing now, which nowPlaying already reports.
        QueueEntry precededBy = matchIndex > 0 ? Build(matchIndex - 1) : null;
        QueueEntry followedBy = matchIndex >= 0 && matchIndex + 1 < timeline.Count ? Build(matchIndex + 1) : null;

        // Say whether the page is the whole queue. Given only queueLength, a caller that expects
        // an entry and doesn't see it wonders whether its limit was too low, and fetches again.
        if (upcoming.Count > 0)
        {
            string coverage = upcoming.Count == timeline.Count
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "This is the whole queue: {0} {1} after the one playing now.",
                    timeline.Count,
                    timeline.Count == 1 ? "track" : "tracks")
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "Showing positions {0}-{1} of {2}. Raise limit to see more, and don't describe these as the whole queue.",
                    first + 1,
                    last,
                    timeline.Count);

            hint = hint == null ? coverage : hint + " " + coverage;
        }
        else if (!lookup && timeline.Count == 0)
        {
            hint = "The queue is empty.";
        }

        // A fact about timing and why it matters, not a sentence to repeat: the caller words it
        // however it likes. The fact alone read as background, and the next track went
        // unmentioned until the hint said why it mattered.
        if (nowPlaying is { RemainingSeconds: < NearlyFinishedSeconds } ending)
        {
            string soon = string.Format(
                CultureInfo.InvariantCulture,
                "The track playing now has {0} seconds left, so it may have finished by the time the listener reads your reply; {1}",
                ending.RemainingSeconds,
                timeline.Count == 0
                    ? "nothing is queued after it yet."
                    : (first == 0 && upcoming.Count > 0 && !upcoming[0].Hidden
                        ? string.Format(CultureInfo.InvariantCulture, "next is \"{0}\" from {1}", upcoming[0].Title, upcoming[0].AlbumTitle)
                        : "position 1 in the queue plays next")
                      + ". Say what plays next as well as what is playing, so the answer is still true when it arrives.");

            hint = hint == null ? soon : hint + " " + soon;
        }

        return new QueueView(now, nowPlaying, upcoming, timeline.Count, at, hint, precededBy, followedBy);

        QueueEntry Build(int i)
        {
            HistoryRow row = timeline[i];
            bool bestMatch = i == matchIndex;
            bool overlaps = window is { } w && row.Played < w.End && row.Played.AddSeconds(row.Length) > w.Start;

            // An entry the caller may not see keeps its place and its timing; only its
            // identity is withheld. Skipping it would leave a hole in the positions and make
            // the queue look shorter than it is.
            return tracks.TryGetValue(row.TrackId, out TrackRow track)
                ? new QueueEntry(i + 1, track.Id, track.Title, track.AlbumTitle, BuildCredits(track), row.Length, row.Played, false, null, bestMatch, overlaps)
                : new QueueEntry(i + 1, null, null, null, [], row.Length, row.Played, true, HiddenQueueNote, bestMatch, overlaps);
        }
    }

    /// <summary>
    /// What has already played, newest first.
    ///
    /// Five shapes, in order of precedence, which is also the order of the parameters:
    /// <paramref name="trackIds"/> returns the most recent play of any of those tracks;
    /// <paramref name="personId"/> the most recent play of anything credited to them; both with
    /// context either side and the play before as EarlierPlay. <paramref name="at"/> returns the
    /// track that was playing around that moment, with context either side;
    /// <paramref name="day"/> the end of that day's playback; none of them the most recent
    /// tracks.
    ///
    /// <paramref name="at"/> is treated as the start of a window <paramref name="atWindowSeconds"/>
    /// long rather than an exact instant, because a clock time is only accurate to the minute
    /// and a track boundary can fall anywhere inside it.
    /// </summary>
    public async Task<HistoryView> GetHistoryAsync(
        int limit, DateTime now, IReadOnlyList<int> trackIds = null, int? personId = null, string role = null, DateTime? at = null, int atWindowSeconds = 60, DateOnly? day = null, bool allowOnlyPublicAlbums = true)
    {
        limit = Math.Clamp(limit, 1, 100);
        atWindowSeconds = Math.Clamp(atWindowSeconds, 1, 3600);

        NowPlaying nowPlaying = await GetNowPlayingAsync(now, allowOnlyPublicAlbums);

        IQueryable<StreamHistory> scope = dbContext.StreamHistory.AsNoTracking();

        List<HistoryRow> rows;
        HistoryRow anchor = null;
        HistoryRow earlierRow = null;
        string hint = null;

        // The requested minute, as one value. Start and end are set together or not at all,
        // so nothing downstream has to infer that one being present means the other is.
        (DateTime Start, DateTime End)? window = null;

        // A lookup by track or by person is one question — which plays count — answered the
        // same way: the most recent of them, including one still in progress, which is then
        // the answer, and the one before it, whatever it was, as earlierPlay.
        IQueryable<StreamHistory> plays = null;
        string noPlaysHint = null;

        if (trackIds is { Count: > 0 })
        {
            // For a song rather than one recording: any of the versions given — an album edit,
            // a radio edit, a remix.
            List<int> ids = trackIds.Distinct().ToList();
            plays = scope.Where(h => ids.Contains(h.TrackStreamInfo.TrackId));
            noPlaysHint = ids.Count == 1
                ? "There's no record of that track having played."
                : "There's no record of any of those tracks having played.";
        }
        else if (personId is { } pid)
        {
            // Anything credited to them, by the same rule a track search uses. A subquery rather
            // than a list of their track IDs, so a prolific artist can't run past a provider's
            // parameter limit.
            IQueryable<int> theirTracks = CreditedTo(dbContext.Tracks, pid, role).Select(t => t.Id);
            plays = scope.Where(h => theirTracks.Contains(h.TrackStreamInfo.TrackId));
            noPlaysHint = "There's no record of anything credited to that person having played.";
        }

        if (plays != null)
        {
            // Two rows, however long the history is: the answer, and the time before it.
            List<HistoryRow> latestTwo = await ProjectHistoryAsync(plays.OrderByDescending(h => h.Played).Take(2));

            if (latestTwo.Count == 0)
            {
                rows = [];
                hint = noPlaysHint;
            }
            else
            {
                HistoryRow latest = latestTwo[0];
                earlierRow = latestTwo.Count > 1 ? latestTwo[1] : null;
                DateTime played = latest.Played;

                int before = Math.Max(1, limit / 2);
                int after = Math.Max(0, limit - before - 1);

                List<HistoryRow> older = await ProjectHistoryAsync(
                    scope.Where(h => h.Played <= played).OrderByDescending(h => h.Played).Take(before + 1));

                List<HistoryRow> newer = await ProjectHistoryAsync(
                    scope.Where(h => h.Played > played).OrderBy(h => h.Played).Take(after + 1));

                rows = newer.Concat(older).OrderByDescending(r => r.Played).ToList();

                // By identity rather than by time. Looking up the minute it started would let a
                // track shorter than a minute lose its own best match to the one after it.
                anchor = rows.FirstOrDefault(r => r.Played == played && r.TrackId == latest.TrackId);
            }
        }
        else if (at is { } point)
        {
            // Split the window around the anchor: the track spanning the requested moment,
            // what led up to it, and what followed.
            int before = Math.Max(1, limit / 2);
            int after = Math.Max(0, limit - before - 1);

            List<HistoryRow> older = await ProjectHistoryAsync(
                scope.Where(h => h.Played <= point).OrderByDescending(h => h.Played).Take(before + 1));

            List<HistoryRow> newer = await ProjectHistoryAsync(
                scope.Where(h => h.Played > point).OrderBy(h => h.Played).Take(after + 1));

            rows = newer.Concat(older).OrderByDescending(r => r.Played).ToList();

            window = (point, point.AddSeconds(atWindowSeconds));
            anchor = ChooseAnchor(rows, point, atWindowSeconds, out bool overlapped);

            if (rows.Count == 0)
            {
                hint = string.Format(
                    CultureInfo.InvariantCulture,
                    "No playback recorded around {0:yyyy-MM-dd HH:mm}. The stream may not have been running then.",
                    point);
            }
            else if (!overlapped)
            {
                hint = string.Format(
                    CultureInfo.InvariantCulture,
                    "Nothing was playing at {0:yyyy-MM-dd HH:mm}; the nearest play is the one flagged, and the rest are context around it.",
                    point);
            }
        }
        else if (day.HasValue)
        {
            DateTime dayStart = day.Value.ToDateTime(TimeOnly.MinValue);
            DateTime dayEnd = dayStart.AddDays(1);

            rows = await ProjectHistoryAsync(
                scope.Where(h => h.Played >= dayStart && h.Played < dayEnd)
                    .OrderByDescending(h => h.Played)
                    .Take(limit + 1));

            if (rows.Count == 0)
            {
                hint = string.Format(
                    CultureInfo.InvariantCulture,
                    "No playback recorded on {0:yyyy-MM-dd}.",
                    dayStart);
            }
        }
        else
        {
            rows = await ProjectHistoryAsync(scope.OrderByDescending(h => h.Played).Take(limit + 1));

            if (rows.Count == 0)
            {
                hint = "No playback has been recorded yet.";
            }
        }

        List<int> neededTracks = rows.Select(r => r.TrackId).ToList();
        if (earlierRow != null)
        {
            neededTracks.Add(earlierRow.TrackId);
        }

        Dictionary<int, TrackRow> tracks = await LoadTrackRowsAsync(neededTracks.Distinct().ToList(), allowOnlyPublicAlbums);

        var entries = new List<HistoryEntry>(rows.Count);
        foreach (HistoryRow row in rows)
        {
            DateTime ended = row.Played.AddSeconds(row.Length);
            bool stillPlaying = ended > now;
            bool bestMatch = anchor != null && ReferenceEquals(row, anchor);

            // Broader than the best match: anything sounding during the requested window, so
            // the caller can say "B was on, though A was still finishing" without comparing
            // timestamps itself.
            bool overlaps = window is { } w && row.Played < w.End && ended > w.Start;

            // The track still playing is reported by nowPlaying. Listing it here as well would
            // describe it as already played and give it an end time in the future. It belongs
            // here only when it answers a point-in-time question, either as the best match or
            // as something else that was sounding during the requested window.
            if (stillPlaying && !bestMatch && !overlaps)
            {
                continue;
            }

            entries.Add(ToEntry(row, bestMatch, overlaps, stillPlaying));
        }

        // The neighbours are picked before trimming, so asking for a single entry still says
        // what was around it. Entries run newest first, so the play before the match sits
        // after it in the list.
        HistoryEntry precededBy = null;
        HistoryEntry followedBy = null;

        int matchIndex = entries.FindIndex(e => e.BestMatch);
        if (matchIndex >= 0)
        {
            precededBy = matchIndex + 1 < entries.Count ? entries[matchIndex + 1] : null;
            followedBy = matchIndex - 1 >= 0 ? entries[matchIndex - 1] : null;
        }

        // Trim to the requested size. For a point in time, keep the window centred on the
        // match: taking the newest entries instead would discard the anchor itself whenever
        // it isn't among them, which is what happens as soon as the limit is small.
        if (entries.Count > limit)
        {
            if (matchIndex < 0)
            {
                entries = entries.Take(limit).ToList();
            }
            else
            {
                int start = Math.Clamp(matchIndex - ((limit - 1) / 2), 0, entries.Count - limit);
                entries = entries.Skip(start).Take(limit).ToList();
            }
        }

        // The earlier play can't be playing now — the flagged one is later — and it answers
        // "when before that", not a question about a moment, so neither flag applies.
        HistoryEntry earlierPlay = earlierRow == null ? null : ToEntry(earlierRow, false, false, false);

        return new HistoryView(now, nowPlaying, entries, at, hint, precededBy, followedBy, earlierPlay);

        // One construction for every entry, so the earlier play is built exactly like the rest,
        // hidden tracks included.
        HistoryEntry ToEntry(HistoryRow row, bool bestMatch, bool overlaps, bool stillPlaying)
        {
            DateTime ended = row.Played.AddSeconds(row.Length);

            // A play the caller may not see keeps its times, so the timeline reads as
            // continuous. Dropping it would leave an unexplained gap that looks like silence.
            return tracks.TryGetValue(row.TrackId, out TrackRow track)
                ? new HistoryEntry(row.Played, ended, track.Id, track.Title, track.AlbumTitle, BuildCredits(track), row.Length, bestMatch, overlaps, stillPlaying, false, null)
                : new HistoryEntry(row.Played, ended, null, null, null, [], row.Length, bestMatch, overlaps, stillPlaying, true, HiddenHistoryNote);
        }
    }

    /// <summary>
    /// The newest StreamHistory row, if it has not finished yet. A row is written when a track
    /// starts, so anything whose start plus length is in the past means the stream is idle.
    /// </summary>
    private async Task<NowPlaying> GetNowPlayingAsync(DateTime now, bool allowOnlyPublicAlbums)
    {
        var current = await dbContext.StreamHistory
            .AsNoTracking()
            .OrderByDescending(h => h.Played)
            .Select(h => new
            {
                h.Played,
                h.TrackStreamInfo.TrackId,
                Length = (int)h.TrackStreamInfo.Track.Length
            })
            .FirstOrDefaultAsync();

        if (current == null)
        {
            return null;
        }

        DateTime ends = current.Played.AddSeconds(current.Length);
        if (ends <= now)
        {
            return null; // nothing playing
        }

        Dictionary<int, TrackRow> track = await LoadTrackRowsAsync([current.TrackId], allowOnlyPublicAlbums);

        int elapsed = (int)Math.Max(0, (now - current.Played).TotalSeconds);
        int remaining = Math.Max(0, current.Length - elapsed);

        // Something is playing either way. Returning null when the caller may not see it
        // would report the stream as idle, which is a different and wrong answer.
        if (!track.TryGetValue(current.TrackId, out TrackRow row))
        {
            return new NowPlaying(null, null, null, [], current.Played, current.Length, elapsed, remaining, true, HiddenNowPlayingNote);
        }

        return new NowPlaying(
            row.Id,
            row.Title,
            row.AlbumTitle,
            BuildCredits(row),
            current.Played,
            current.Length,
            elapsed,
            remaining,
            false,
            null);
    }

    /// <summary>
    /// Pick the one play that best answers "what was on at T".
    ///
    /// <para>A clock time has minute granularity, so T is treated as the start of a window
    /// rather than a knife-edge instant: a track beginning a second after T occupies almost
    /// the whole minute the listener meant, while the one it replaced occupies almost none of
    /// it. The entry covering the most of the window wins.</para>
    ///
    /// <para>When nothing overlaps at all — a real gap in playback — the nearest play is
    /// chosen instead, so the caller always has a single entry to talk about. Whether it
    /// overlapped is reported separately, because "this was on" and "nothing was on, but this
    /// was closest" are different answers.</para>
    /// </summary>
    private static HistoryRow ChooseAnchor(List<HistoryRow> rows, DateTime point, int windowSeconds, out bool overlapped)
    {
        overlapped = false;

        if (rows.Count == 0)
        {
            return null;
        }

        DateTime windowEnd = point.AddSeconds(windowSeconds);

        HistoryRow best = null;
        double bestOverlap = 0;

        foreach (HistoryRow row in rows)
        {
            DateTime ended = row.Played.AddSeconds(row.Length);
            double overlap = (Min(ended, windowEnd) - Max(row.Played, point)).TotalSeconds;

            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                best = row;
            }
        }

        if (best != null)
        {
            overlapped = true;
            return best;
        }

        // Nothing was playing then. Fall back to whichever play sits closest to the window,
        // measured from whichever edge of the track is nearer.
        double bestDistance = double.MaxValue;

        foreach (HistoryRow row in rows)
        {
            DateTime ended = row.Played.AddSeconds(row.Length);
            double distance = row.Played > windowEnd
                ? (row.Played - windowEnd).TotalSeconds
                : (point - ended).TotalSeconds;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = row;
            }
        }

        return best;
    }

    /// <summary>
    /// A moment as a clock time, naming the day when it isn't today. A clock time asked of
    /// the queue that has already passed rolls over to tomorrow, and a bare "21:15" in a hint
    /// would hide that from the caller.
    /// </summary>
    private static string DescribeMoment(DateTime moment, DateTime now)
    {
        string clock = moment.ToString("HH:mm", CultureInfo.InvariantCulture);

        if (moment.Date == now.Date)
        {
            return clock;
        }

        return moment.Date == now.Date.AddDays(1)
            ? clock + " tomorrow"
            : moment.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    /// <summary>
    /// The tracks and albums the stream's repeat rules keep out for now. The one definition
    /// both picking and adding use, so an add can't get round what a pick refuses.
    /// </summary>
    private async Task<(List<int> Tracks, List<int> Albums)> GetRepeatExclusionsAsync(PickRules rules, DateTime now)
    {
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

        return (trackExclusions, albumExclusions);
    }

    // A repeat window in words, for a skip reason the caller can pass on.
    private static string DescribeWindow(int minutes) => minutes switch
    {
        >= 2 * 1440 => string.Format(CultureInfo.InvariantCulture, "{0:0} days", Math.Round(minutes / 1440.0)),
        >= 120 => string.Format(CultureInfo.InvariantCulture, "{0:0} hours", Math.Round(minutes / 60.0)),
        60 => "hour",
        _ => string.Format(CultureInfo.InvariantCulture, "{0} minutes", minutes),
    };

    /// <summary>
    /// Tracks credited to a person, at track level or through their album. The one rule every
    /// person-scoped lookup uses, so "credited to" means the same thing wherever it appears:
    /// a track on a soundtrack credited to its composer counts even with no credit of its own.
    /// </summary>
    private static IQueryable<Track> CreditedTo(IQueryable<Track> tracks, int personId, string role) =>
        tracks.Where(t =>
            t.TrackPersonGroupPersonRelations.Any(r =>
                (role == null || r.PersonGroup.Name == role) &&
                r.Persons.Any(p => p.Id == personId))
            ||
            t.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                (role == null || r.PersonGroup.Name == role) &&
                r.Persons.Any(p => p.Id == personId)));

    private static Task<List<HistoryRow>> ProjectHistoryAsync(IQueryable<StreamHistory> query) =>
        query
            .Select(h => new HistoryRow
            {
                Played = h.Played,
                TrackId = h.TrackStreamInfo.TrackId,
                Length = h.TrackStreamInfo.Track.Length
            })
            .ToListAsync();

    private async Task<Dictionary<int, TrackRow>> LoadTrackRowsAsync(List<int> trackIds, bool allowOnlyPublicAlbums)
    {
        if (trackIds.Count == 0)
        {
            return [];
        }

        IQueryable<Track> query = dbContext.Tracks.AsNoTracking().Where(t => trackIds.Contains(t.Id));

        if (allowOnlyPublicAlbums)
        {
            query = query.Where(t => t.Disc.Album.IsPublic);
        }

        List<TrackRow> rows = await query
            .Select(TrackRowSelector)
            .AsSplitQuery()
            .ToListAsync();

        return rows.ToDictionary(r => r.Id);
    }

    // Credits as returned to callers, leaving out any role that has nobody in it.
    private static List<CreditDto> ToCredits(IEnumerable<CreditRow> rows, CreditLevel appliesTo) =>
        rows
            .Where(c => c.Persons.Count > 0)
            .Select(c => new CreditDto(c.Role, c.Persons.Select(FormatName).ToList(), appliesTo))
            .ToList();

    private static List<CreditDto> BuildCredits(TrackRow r)
    {
        var credits = new List<CreditDto>();

        foreach (CreditRow c in r.TrackCredits)
        {
            credits.Add(new CreditDto(c.Role, c.Persons.Select(FormatName).ToList(), CreditLevel.Track));
        }

        foreach (CreditRow c in r.AlbumCredits)
        {
            credits.Add(new CreditDto(c.Role, c.Persons.Select(FormatName).ToList(), CreditLevel.Album));
        }

        return credits;
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
        PickRules rules,
        DateTime now,
        int? position = null,
        bool playNow = false,
        bool allowIgnoringRules = false,
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

        // The repeat rules bind every add unless the caller may set them aside, explicit
        // requests included. They are the pick's own rules, measured from the end of the queue;
        // for an insert nearer the front that is slightly stricter than it needs to be, never
        // laxer.
        var excludedTracks = new HashSet<int>();
        var excludedAlbums = new HashSet<int>();
        var albumOf = new Dictionary<int, int>();

        if (!allowIgnoringRules && infos.Count > 0)
        {
            (List<int> tracks, List<int> albums) = await GetRepeatExclusionsAsync(rules, now);
            excludedTracks = new HashSet<int>(tracks);
            excludedAlbums = new HashSet<int>(albums);

            List<int> found = [.. infos.Keys];
            albumOf = await dbContext.TrackStreamInfos
                .Where(x => found.Contains(x.TrackId))
                .Select(x => new { x.TrackId, x.Track.Disc.AlbumId })
                .ToDictionaryAsync(x => x.TrackId, x => x.AlbumId);
        }

        string trackRuleReason = $"It has played within the last {DescribeWindow(rules.MinutesBetweenTrackRepeat)} or is already queued, so the stream's repeat rules keep it out for now.";
        string albumRuleReason = $"Its album has played within the last {DescribeWindow(rules.MinutesBetweenAlbumRepeat)} or is already queued, so the stream's repeat rules keep it out for now.";
        int ruleSkips = 0;

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

            if (excludedTracks.Contains(id))
            {
                skipped.Add(new SkippedTrack(id, trackRuleReason));
                ruleSkips++;
                continue;
            }

            if (albumOf.TryGetValue(id, out int albumId) && excludedAlbums.Contains(albumId))
            {
                skipped.Add(new SkippedTrack(id, albumRuleReason));
                ruleSkips++;
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

        int? firstAddedPosition = null;
        if (toAdd.Count > 0)
        {
            ushort firstSort = toAdd[0].SortOrder;
            firstAddedPosition = await dbContext.StreamQueue.CountAsync(s => s.SortOrder < firstSort) + 1;
        }

        // When the rules kept everything out, say so in the hint as well as per track: the
        // tempting next move is to queue something else in its place.
        string hint = BuildQueueHint(firstAddedPosition, queueLength, playNow, added.Count)
            ?? (added.Count == 0 && ruleSkips > 0
                ? "Nothing was added: the stream's repeat rules keep these tracks out for now. Tell the listener so, rather than queueing something else in their place."
                : null);

        return new QueueAddResult(added, skipped, queueLength, hint, firstAddedPosition);
    }

    /// <summary>
    /// A true sentence about when the added tracks will play, so the caller has no reason to
    /// improvise one. Position claims are the thing callers most reliably get wrong: "up next"
    /// for something at the back of a long queue, or a place in the order worked out from what
    /// they queued earlier rather than from the queue itself.
    /// </summary>
    private static string BuildQueueHint(int? firstAddedPosition, int queueLength, bool playNow, int addedCount)
    {
        if (addedCount == 0 || firstAddedPosition is not { } position)
        {
            return null;
        }

        string subject = addedCount == 1 ? "This track" : $"These {addedCount} tracks";

        if (playNow)
        {
            return $"{subject} started playing immediately, interrupting whatever was on.";
        }

        if (position <= 1)
        {
            return $"{subject} will play next, as soon as the current track finishes.";
        }

        string it = addedCount == 1 ? "it" : "the first of them";

        return $"{subject} went to position {position} of {queueLength}. There are {position - 1} tracks ahead, so do not say {it} is up next or playing soon: say where in the queue it is, or call playlist_tools__get_queue with its ID in trackIds if the user wants a time.";
    }

    // ---------- Helpers ----------

    private static TrackCandidate ToCandidate(TrackRow r, double score, string matchedOn)
    {
        return new TrackCandidate(
            r.Id, r.Title, r.TrackNumber, r.DiscNumber, r.AlbumId, r.AlbumTitle,
            r.Year, r.LengthSeconds, BuildCredits(r), Math.Round(score, 3), matchedOn);
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

    private static CreditLevel ToCreditLevel(PersonGroupType personGroup)
    {
        return personGroup switch
        {
            PersonGroupType.Album => CreditLevel.Album,
            PersonGroupType.Track => CreditLevel.Track,
            _ => throw new ArgumentOutOfRangeException(nameof(personGroup), personGroup, null)
        };
    }
}