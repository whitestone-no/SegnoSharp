using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Models;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Models.Enums;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Helpers;
using Whitestone.SegnoSharp.Shared.Helpers.Security;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Services;

// ReSharper disable EntityFramework.ClientSideDbFunctionCall - These warnings are not really client side as they are inside predicates and are mistakenly interpreted as client side. The predicate itself is used inside a query.

public interface IMusicSearchService
{
    Task<IReadOnlyList<RoleResult>> GetRolesAsync();
    Task<IReadOnlyList<PersonResult>> SearchPeopleAsync(string query, string role = null, int limit = 5, bool allowOnlyPublicAlbums = true);
    Task<IReadOnlyList<AlbumResult>> SearchAlbumsAsync(string query, int limit = 5, bool allowOnlyPublicAlbums = true);
    Task<TrackSearchResult> SearchTracksAsync(TrackSearchQuery query, double minScore = 0.4, bool allowOnlyPublicAlbums = true);
    Task<AlbumTracklist> GetAlbumTracklistAsync(int albumId, bool allowOnlyPublicAlbums = true);
    Task<QueueAddResult> AddTracksToQueueAsync(IReadOnlyList<int> trackIds, int? position = null, bool playNow = false);
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
    // library into memory. Hitting the cap a lot is a signal the query is too broad.
    private const int TrackCandidateCap = 300;
    private const int NameCandidateCap = 200;

    public async Task<IReadOnlyList<RoleResult>> GetRolesAsync()
    {
        List<RoleResult> roles = await dbContext.PersonGroups
            .Select(pg => new RoleResult(pg.Name, ToCreditSource(pg.Type)))
            .ToListAsync();

        return roles;
    }

    /// <summary>
    /// Resolve a person name to candidates, with per-role credit counts and sample works
    /// so the agent can tell two same-named people apart (the film composer vs the guitarist).
    /// A genuine tie (two distinct people) is the agent's cue to ask.
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
            .Take(NameCandidateCap)
            .ToListAsync();

        // Rank in memory against the full name, then keep the top few.
        var ranked = raw
            .Select(p => new
            {
                p.Id,
                p.Version,
                Name = FormatName(p.FirstName, p.LastName, p.Version),
                TextSearch.ScoreTitle(query, $"{p.FirstName} {p.LastName}").Score
            })
            .OrderByDescending(x => x.Score)
            .Take(limit)
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
        var results = new List<PersonResult>(ranked.Count);

        foreach (var person in ranked)
        {
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

    /// <summary>Resolve an album title to candidates, with album-level credits for disambiguation.</summary>
    public async Task<IReadOnlyList<AlbumResult>> SearchAlbumsAsync(string query, int limit = 5, bool allowOnlyPublicAlbums = true)
    {
        List<string> tokens = TextSearch.Tokenize(query);
        if (tokens.Count == 0)
        {
            return [];
        }

        Expression<Func<Album, bool>> predicate = PredicateBuilder.False<Album>();
        foreach (string token in tokens)
        {
            string pattern = "%" + token + "%";
            predicate = predicate.Or(a => EF.Functions.Like(a.Title.ToLower(), pattern));
        }

        // Projects a nested collection (Credits -> Persons); AsSplitQuery keeps EF from
        // multiplying rows across the two collection levels. OrderBy makes the Take stable.
        IQueryable<Album> albumQuery = dbContext.Albums
            .Where(predicate);

        if (allowOnlyPublicAlbums)
        {
            albumQuery = albumQuery.Where(a => a.IsPublic);
        }

        var raw = await albumQuery
            .OrderBy(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Published,
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
            })
            .Take(NameCandidateCap)
            .AsSplitQuery()
            .ToListAsync();

        return raw
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Published,
                a.Credits,
                TextSearch.ScoreTitle(query, a.Title).Score
            })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(a => new AlbumResult(
                a.Id,
                a.Title,
                a.Published,
                a.Credits.Select(c => new CreditDto(
                    c.Role,
                    c.Persons.Select(FormatName).ToList(),
                    CreditSource.Album)).ToList(),
                Math.Round(a.Score, 3)))
            .ToList();
    }

    // ---------- Track search (the crux) ----------

    /// <summary>
    /// Search playable tracks by any combination of title, person/role and album.
    /// Person scope unions direct track credits with credits inherited from the album,
    /// so "by John Williams" still finds tracks credited only at the soundtrack level.
    /// The <see cref="TrackSearchResult.Outcome"/> plus <paramref name="minScore"/> let the
    /// agent branch: Matched → act; WeakMatch/NoMatch → resolve the exact title externally
    /// and search again.
    /// </summary>
    public async Task<TrackSearchResult> SearchTracksAsync(TrackSearchQuery query, double minScore = 0.4, bool allowOnlyPublicAlbums = true)
    {
        IQueryable<Track> tracks = dbContext.Tracks.Where(t => t.TrackStreamInfo != null); // playable only

        if (allowOnlyPublicAlbums)
        {
            tracks = tracks.Where(t => t.Disc.Album.IsPublic);
        }

        if (query.AlbumId.HasValue)
        {
            int albumId = query.AlbumId.Value;
            tracks = tracks.Where(t => t.Disc.AlbumId == albumId);
        }

        if (query.PersonId.HasValue)
        {
            int personId = query.PersonId.Value;
            string role = query.Role;
            tracks = tracks.Where(t =>
                t.TrackPersonGroupPersonRelations.Any(r =>
                    (role == null || r.PersonGroup.Name == role) &&
                    r.Persons.Any(p => p.Id == personId))
                ||
                t.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                    (role == null || r.PersonGroup.Name == role) &&
                    r.Persons.Any(p => p.Id == personId)));
        }

        List<string> tokens = string.IsNullOrWhiteSpace(query.TitleQuery)
            ? new List<string>()
            : TextSearch.Tokenize(query.TitleQuery);

        if (tokens.Count > 0)
        {
            tracks = tracks.Where(TextSearch.TitleLike(tokens));
        }

        // Two collection projections (TrackCredits and AlbumCredits, each nesting Persons):
        // AsSplitQuery loads each in its own round-trip so a single query doesn't multiply
        // rows. OrderBy(Id) makes the candidate Take deterministic (and is required for a
        // stable split-query correlation). Final ranking happens in memory below.
        List<TrackRow> rows = await tracks
            .OrderBy(t => t.Id)
            .Select(TrackRowSelector)
            .Take(TrackCandidateCap)
            .AsSplitQuery()
            .ToListAsync();

        bool hasTitle = tokens.Count > 0;

        List<(TrackRow Row, double Score, string MatchedOn)> scored;
        if (hasTitle)
        {
            scored = rows
                .Select(r =>
                {
                    (double score, string matchedOn) = TextSearch.ScoreTitle(query.TitleQuery, r.Title);
                    return (r, score, matchedOn);
                })
                .OrderByDescending(x => x.score)
                .ToList();
        }
        else
        {
            // Structural search ("something by X"): no title to rank on, so order for
            // stable output and treat every credited hit as a full match.
            scored = rows
                .OrderBy(r => r.AlbumTitle).ThenBy(r => r.DiscNumber).ThenBy(r => r.TrackNumber)
                .Select(r => (r, 1.0, "credit match"))
                .ToList();
        }

        double? topScore = scored.Count == 0 ? null : scored[0].Score;

        SearchOutcome outcome;
        if (scored.Count == 0)
        {
            outcome = SearchOutcome.NoMatch;
        }
        else if (!hasTitle || topScore >= minScore)
        {
            outcome = SearchOutcome.Matched;
        }
        else
        {
            outcome = SearchOutcome.WeakMatch;
        }

        string hint = outcome switch
        {
            SearchOutcome.NoMatch when hasTitle =>
                "No track title matched in this scope. Resolve the exact track name (e.g. via external lookup) and search again. If the album is known get all tracks from the album for the real titles.",
            SearchOutcome.NoMatch =>
                "No credited, playable tracks found for this person/role.",
            SearchOutcome.WeakMatch =>
                "Only weak title matches. Verify the exact track name before adding, or fetch the album tracklist to choose.",
            _ => null
        };

        List<TrackCandidate> candidates = scored
            .Take(query.Limit)
            .Select(x => ToCandidate(x.Row, x.Score, x.MatchedOn))
            .ToList();

        return new TrackSearchResult(
            outcome,
            candidates,
            topScore.HasValue ? Math.Round(topScore.Value, 3) : null,
            hint,
            query.AlbumId);
    }

    /// <summary>
    /// Full disc/track tree for one album, including unplayable tracks (marked), so the
    /// agent can verify an externally-suggested title against what actually exists.
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
    /// </summary>
    public async Task<QueueAddResult> AddTracksToQueueAsync(IReadOnlyList<int> trackIds, int? position = null, bool playNow = false)
    {
        var added = new List<int>();
        var skipped = new List<SkippedTrack>();

        if (trackIds.Count == 0)
        {
            int emptyLen = await dbContext.StreamQueue.CountAsync();
            return new QueueAddResult(added, skipped, emptyLen);
        }

        // One lookup for all requested tracks: TrackStreamInfo is the playability gate.
        Dictionary<int, TrackStreamInfo> infos = await dbContext.TrackStreamInfos
            .Where(x => trackIds.Contains(x.TrackId))
            .ToDictionaryAsync(x => x.TrackId);

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
                skipped.Add(new SkippedTrack(id, "Track not found or has no playable stream (missing TrackStreamInfo)."));
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
                s.SortOrder = (ushort)(s.SortOrder + toAdd.Count);
            }
        }

        dbContext.StreamQueue.AddRange(toAdd);
        await dbContext.SaveChangesAsync();

        await cambion.PublishEventAsync(new PlaylistUpdated());

        if (playNow)
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