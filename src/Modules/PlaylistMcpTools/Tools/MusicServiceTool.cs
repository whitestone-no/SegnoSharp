using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;
using Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Services;
using Whitestone.SegnoSharp.Shared.Attributes.Security;
using Whitestone.SegnoSharp.Shared.Helpers.Security;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Permissions;
// ReSharper disable UnusedMember.Global - ReSharper reports that methods are not use, but these methods are just exposed externally, never called by SegnoSharp itself.

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Tools;

[McpServerToolType]
public class MusicServiceTool(
    ILogger<MusicServiceTool> logger,
    PermissionAuthorizer permissionAuthorizer,
    IMusicSearchService musicSearchService,
    ISystemClock systemClock,
    IRandomGenerator randomGenerator)
{
    // The auto-playlist's repeat rules are not reachable from this module yet, so they are
    // mirrored here rather than read from settings. If you tune the auto-playlist (see
    // GetNextTrackAsync), change these to match or picks will drift out of step with the
    // rest of the stream. Extracting a shared settings source would remove the duplication.
    private const int MinutesBetweenTrackRepeat = 15480; // ~10.75 days
    private const int MinutesBetweenAlbumRepeat = 60;
    private const bool UseWeightedPicks = true;

    // A requested moment is accurate to the minute, so it is matched as a minute-long window
    // and the track covering most of it wins, rather than whatever happened to be on at second
    // zero. Shared by history, looking back, and the queue, looking ahead.
    private const int PointInTimeWindowSeconds = 60;

    // A large add monopolises a shared stream, so past this many tracks the call is refused
    // until the caller has told the user the size and been told to go ahead. Past the smaller
    // threshold it succeeds but is told to mention the count, because a rule carried back in
    // the response is followed far more often than one in a system prompt.
    // How many results a tool returns when the caller doesn't say, and the most it will return.
    // A generous default saves a follow-up call: callers told a page was only part of the
    // matches have tended to accept it rather than ask for more. The Description attributes
    // below repeat these numbers, since attribute text can't be built from an int constant,
    // so change the two together.
    private const int DefaultResultLimit = 20;
    private const int MaxResultLimit = 100;

    // People cost several queries each (credit counts and sample works, one person at a time),
    // so a search matching many of them is far more expensive per result than the others. Past
    // 25 people sharing a name, the right move is to ask which one anyway.
    private const int MaxPeopleLimit = 25;

    // The queue and the history are views either side of now, and most questions about them
    // only need what's nearest: what's next, or what just played. How long the queue gets also
    // depends on the installation. A moment further away is asked for with minutesAhead or
    // minutesAgo, which centre the page on it, rather than by paging from the start.
    private const int DefaultPlaybackLimit = 10;

    private const int ConfirmLargeAddThreshold = 25;
    private const int AnnounceAddThreshold = 10;

    [McpServerTool(ReadOnly = true), Description("List every credit role that can be passed as a 'role' filter, with appliesTo: whether it is used for albums, tracks or both, the same values a credit's appliesTo uses. Call this first if you are unsure which role values are valid, never pass a role string that did not come from here, and do not assume the list is fixed.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<RoleResult>> GetRoles()
    {
        return await musicSearchService.GetRolesAsync();
    }

    [McpServerTool(ReadOnly = true), Description("Resolve a person/group name to candidates with per-role credit counts and sample works. Use creditCounts and sampleWorks to tell same-named people apart (the film composer versus the guitarist); a number in parentheses after a name marks a second person with that name. If several fit and nothing tells them apart, don't pick silently: ask the listener which is meant, with a tool for asking if you have one, or say which you chose and name the other.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<PersonResult>> SearchPeople(
        ClaimsPrincipal user,
        [Description("Name of the person or group to search for, e.g. 'Beethoven' or 'The Beatles'. Search the name as the listener wrote it first; if that finds nothing, the library may use another spelling of the same name ('Tchaikovsky' for 'Tsjajkovskij'), so try that.")] string query,
        [Description("Optional role filter. Candidates with no credit in this role are excluded, and the returned creditCounts and sampleWorks are narrowed to it. Valid values come from playlist_tools__get_roles. Call playlist_tools__get_roles if unsure. Do not invent other values.")] string role = null,
        [Description("Maximum number of candidate people/groups to return (1-25).")] int limit = DefaultResultLimit)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new McpException("The query parameter is required.");
        }

        limit = Math.Clamp(limit, 1, MaxPeopleLimit);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.SearchPeopleAsync(query, role, limit, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("Resolve albums by title, by credited person, or both; at least one of query and personId is required. Returns album-level credits, a match score, and totalMatches: the number of albums that matched before the limit was applied. When totalMatches is larger than the list you got back, you are holding a slice, not the whole set, and hint says so: raise limit or say how many there are. If truncated is true, a title search matched more albums than the ranking looks at, so the results came from a subset: search a more specific title or add a personId rather than trusting the order. When an album has no album-level credits, which is usual for compilations, a note says so: never describe a track's artist as the album's. Without a title, results come back in title order. Use personId on its own to answer 'what else do we have by X'; do not group the results of a track search to answer that, because a track search stops at its own limit and will under-report. Prefer the plainest exact title match: sequels ('... II') and qualified editions ('... (Complete Rejected Score)', deluxe, live) should only be chosen when the user asked for them.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<AlbumSearchResult> SearchAlbums(
        ClaimsPrincipal user,
        [Description("Optional album title to search for, in the listener's own words as written: don't translate or rephrase them. Leave empty to match on personId only.")] string query = null,
        [Description("Optional person/group ID to filter by, as returned by playlist_tools__search_people. Matches albums credited to them at album level, and albums carrying a track credited to them. Use 0 or omit to not filter by person.")] int personId = 0,
        [Description("Optional role filter. Valid values come from playlist_tools__get_roles. Call playlist_tools__get_roles if unsure. Do not invent other values. Only meaningful together with personId.")] string role = null,
        [Description("Maximum number of albums to return (1-100). Compare it against totalMatches before calling a list complete.")] int limit = DefaultResultLimit)
    {
        int? personIdFilter = personId > 0 ? personId : null;

        if (!string.IsNullOrWhiteSpace(role) && personIdFilter is null)
        {
            throw new McpException("The 'role' filter only works together with 'personId'. To filter by a named artist, first call playlist_tools__search_people to resolve them to a personId, then pass that personId here.");
        }

        if (string.IsNullOrWhiteSpace(query) && personIdFilter is null)
        {
            throw new McpException("Supply at least one of 'query' or 'personId'. An unfiltered album search returns arbitrary albums.");
        }

        limit = Math.Clamp(limit, 1, MaxResultLimit);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.SearchAlbumsAsync(query, personIdFilter, role, limit, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("Search playable tracks by any combination of title, person/group, role and album. Each result carries its album, year, length and full credits, so no extra lookup is needed to describe it. At least one of titleQuery, personId or albumId is required. A title search looks across the whole library, where the same title often exists on several albums, so resolve the person with playlist_tools__search_people or the album with playlist_tools__search_albums first and pass personId or albumId whenever you know them; narrow further if the result comes back with truncated true. Search here before searching the web, with the listener's own words, even when they sound like a description: 'the mission theme for NBC news' is a real title. Use web search only if no title here matches what they said. When the request names a film, show or franchise, prefer a track from an album named after it over a compilation track whose title matches the words better: the main title on a Star Wars album beats 'Main Theme from Star Wars' on a compilation, which is usually a re-recording. If only a compilation track matches, look for the film's own album with playlist_tools__search_albums before settling. To say who is on an album, use playlist_tools__get_album_tracklist: a track search returns only the tracks that matched, not the whole album. A question about who or what is on something, such as 'who's on X?', is about an album: start with playlist_tools__search_albums. outcome reports how the search went. Matched means at least one candidate reached minScore and is returned; WeakMatch means candidates exist but none reached minScore, so nothing is returned and topScore plus hint say how close the best one came; NoMatch means nothing exists in this scope. A search with no titleQuery has nothing to rank against, so anything credited comes back Matched. Read hint for the next step.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<TrackSearchResult> SearchTracks(
        ClaimsPrincipal user,
        [Description("Optional track title to match, in the listener's own words as written: don't translate ('Snøfall' is not 'Snowfall') or rephrase them. When an outside lookup has given you the exact title, search for all of it rather than a fragment. Don't swap their words for a title you remember, as in 'theme' becoming 'Main Title': if you think you know the piece, confirm it by searching the web, with a tool for searching if you have one, then search for the title it gives. Do these one after the other, not side by side: what you search for depends on what the web says. Leave empty to match on the other filters only.")] string titleQuery = null,
        [Description("Optional person/group ID to filter by, as returned by playlist_tools__search_people. If you have already resolved the person in this conversation, pass it rather than searching the title across the whole library. Use 0 or omit to not filter by person.")] int personId = 0,
        [Description("Optional role filter. Valid values come from playlist_tools__get_roles. Call playlist_tools__get_roles if unsure. Do not invent other values. Only meaningful together with personId. Leave it out unless you need to separate two different people: a person search already includes credits inherited from the album.")] string role = null,
        [Description("Optional album ID to filter by, as returned by playlist_tools__search_albums. If you have already resolved the album in this conversation, pass it rather than searching the title across the whole library. Use 0 or omit to not filter by album.")] int albumId = 0,
        [Description("Maximum number of tracks to return (1-100). Compare it against totalMatches before calling a list complete.")] int limit = DefaultResultLimit,
        [Description("Minimum fuzzy-match score in the range 0.0-1.0. Candidates below it are excluded, so raising it narrows the results and lowering it widens them. Leave at the default for loose phrasing; raise it to about 0.7 when you believe you have the exact title. Do not go below 0.3, where matches stop being meaningful. Retry a WeakMatch once at most, with the value suggested by hint.")] double minScore = 0.4)
    {
        // Translate the MCP-facing sentinels (0 = not supplied) to the service's nullable contract.
        int? personIdFilter = personId > 0 ? personId : null;
        int? albumIdFilter = albumId > 0 ? albumId : null;

        if (!string.IsNullOrWhiteSpace(role) && personIdFilter is null)
        {
            throw new McpException("The 'role' filter only works together with 'personId'. To filter by a named artist, first call playlist_tools__search_people to resolve them to a personId, then pass that personId here.");
        }

        if (string.IsNullOrWhiteSpace(titleQuery) && personIdFilter is null && albumIdFilter is null)
        {
            throw new McpException("Supply at least one of 'titleQuery', 'personId' or 'albumId'. An unfiltered search returns arbitrary tracks and cannot rank them, so its results are meaningless.");
        }

        limit = Math.Clamp(limit, 1, MaxResultLimit);
        minScore = Math.Clamp(minScore, 0.0, 1.0);

        TrackSearchQuery query = new(titleQuery, personIdFilter, role, albumIdFilter, limit);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.SearchTracksAsync(query, minScore, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("Full disc/track tree for one album, in order, including unplayable tracks (isPlayable false): for verifying an externally-suggested title against what actually exists, for queueing a complete album, and for saying who is on an album. Credits for the album as a whole are listed once and apply to every track; each track carries only its own. On a compilation the album usually has none, which hint says, and the track credits name each performer. Never pass a track with isPlayable false to playlist_tools__add_to_queue. If the listener described a piece rather than naming it, as in 'the theme from X', and you are about to choose one of these titles because you believe it's the well-known one, verify that by searching the web first, with a tool for searching if you have one, and wait for its answer before choosing: a pick from memory is a guess, and so is one made before the web replies.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<AlbumTracklist> GetAlbumTracklist(
        ClaimsPrincipal user,
        [Description("The album ID whose full disc/track tree to retrieve, as returned by playlist_tools__search_albums or playlist_tools__search_tracks.")] int albumId)
    {
        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        AlbumTracklist tracklist = await musicSearchService.GetAlbumTracklistAsync(albumId, allowOnlyPublicAlbums);

        if (tracklist == null)
        {
            throw new McpException("No album with that ID is available. It may not exist, or may not be visible to you. Search for the album again rather than trying other IDs.");
        }

        return tracklist;
    }

    [McpServerTool(ReadOnly = true), Description("Pick one or more random tracks for open-ended requests: by a person or group ('play something by X'), from an album ('play something from Gladiator'), or both together ('something from Gladiator by Hans Zimmer'). Prefer this to choosing from a search yourself: a search returns its first page, so a choice from it isn't random and can't reach the rest. Honours the stream's no-repeat rules. When they leave nothing to pick, it may relax them itself, one at a time, depending on this connection's permissions. hint says when that happened, so the pick may have played recently, or why picks is empty: nothing matches at all, or everything that matches has played recently. poolSize is how many eligible tracks it chose from. Picks are always playable, and the score on a pick is not a match quality — ignore it.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<TrackPickResult> PickTracks(
        ClaimsPrincipal user,
        [Description("Optional: the person or group to pick from, as returned by playlist_tools__search_people. At least one of personId and albumId is required.")] int personId = 0,
        [Description("Optional: the album to pick from, as returned by playlist_tools__search_albums. Together with personId, picks only tracks on that album credited to them.")] int albumId = 0,
        [Description("Optional role filter for personId. Valid values come from playlist_tools__get_roles. Call playlist_tools__get_roles if unsure. Do not invent other values. Only meaningful together with personId.")] string role = null,
        [Description("Number of tracks to pick (1-50). Leave at 1 unless the user asked for several: 'play something by X' and 'put on some X' both mean one track. Fewer may come back than requested if the eligible pool is smaller.")] int count = 1)
    {
        int? personFilter = personId > 0 ? personId : null;
        int? albumFilter = albumId > 0 ? albumId : null;

        if (personFilter is null && albumFilter is null)
        {
            throw new McpException("Supply personId, albumId or both: a person from playlist_tools__search_people, an album from playlist_tools__search_albums.");
        }

        if (!string.IsNullOrWhiteSpace(role) && personFilter is null)
        {
            throw new McpException("The 'role' filter only works together with 'personId'. To filter by a named person, first call playlist_tools__search_people to resolve them to a personId, then pass that personId here.");
        }

        // Clamp to a sane range. The service also floors count at 1, but we enforce both bounds here explicitly.
        count = Math.Clamp(count, 1, 50);

        PickRules rules = RepeatRules();

        bool allowIgnoringRules = await permissionAuthorizer.HasAnyAsync(user, CorePermissions.PlaylistRulesIgnore);
        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.PickTracksAsync(
            rules,
            systemClock.Now,
            randomGenerator.GetInt,
            personFilter,
            personFilter.HasValue ? role : null,
            albumFilter,
            count,
            allowIgnoringRules,
            allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("What is playing right now and what is queued behind it. Returns serverTime (the clock everything here is relative to), nowPlaying with how many seconds are elapsed and remaining, the next entries with their position and estimated start time, and queueLength, the number of tracks waiting behind the one playing now. The playing track is never counted in it, and positions start at 1 for the track that plays next. Use this for 'what is this', 'what is next', 'what is coming up'. For 'when will [track] play' or 'is it still queued', pass trackIds rather than paging through the queue looking for it. hint always says whether upcoming is the whole queue or only part of it. The queue moves constantly — tracks finish and the next one starts, tracks are added automatically, other people add and remove — so two calls can disagree without anything being wrong: compare their serverTime. Give minutesAhead for 'what's playing in an hour', or time for 'what will play at 21:15': the entry expected then carries bestMatch, overlapsTargetTime marks everything expected during that minute, precededBy and followedBy name its neighbours, and targetTime echoes the moment used — the same fields get_history returns for a moment past. If the moment is still inside the track playing now, or lies past the end of the queue, upcoming is empty and hint says which. Past the end, tracks haven't been chosen yet, so never name the last entry as the answer. estimatedStart assumes back-to-back playback: treat it as reliable for the next few entries and as a rough guide further out. Nothing records who or what added an entry, so never say a track was requested by anyone, not even one you queued yourself. An entry with hidden true is a real track on an album you may not see: it keeps its position and timing but its title and artist are withheld. Positions are always contiguous, so a hidden entry is never a gap or an error. Its note field explains this in words: relay that, never guess what the track is, and never leave the entry out when listing what is coming up.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<QueueView> GetQueue(
        ClaimsPrincipal user,
        [Description("How many upcoming entries to return (1-100). Compare against queueLength before calling the list complete. For a moment ahead, they are split either side of it.")] int limit = DefaultPlaybackLimit,
        [Description("Optional: how many minutes from now to look, for 'what's playing in an hour'. Returns the entry expected to be playing then, with context either side. Takes precedence over time. Use 0 or omit to see what's next.")] int minutesAhead = 0,
        [Description("Optional clock time as HH:mm, 24-hour, server-local, for 'what will play at 21:15'. Means the next time the clock reads that: later today, or tomorrow if today's has already passed. If the listener's time could be read two ways, as '9:15' can, use whichever comes next from now, and say which you used so they can correct it.")] string time = null,
        [Description("Optional: track IDs from earlier results, for 'when will it play' or 'is it still queued'. Pass several for an album, or for every version of a song: whichever of them comes first in the queue is flagged bestMatch, with its position and estimated start, and hint says how many of them are queued and whether one is playing now. A track is found wherever it now sits, so if someone has moved it, its position differs from where you queued it — that is the queue changing, not an error. If none of them is in the queue, upcoming is empty and hint says so. Takes precedence over minutesAhead and time. At most 100.")] List<int> trackIds = null)
    {
        limit = Math.Clamp(limit, 1, MaxResultLimit);

        DateTime now = systemClock.Now;
        DateTime? at = null;
        List<int> trackFilter = NormalizeTrackIds(trackIds);

        // Looking up a track is a different question from looking up a moment, so the moment
        // parameters are ignored rather than combined with it.
        if (trackFilter is null && minutesAhead > 0)
        {
            at = now.AddMinutes(minutesAhead);
        }
        else if (trackFilter is null && !string.IsNullOrWhiteSpace(time))
        {
            if (!TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
            {
                throw new McpException("The 'time' parameter must be formatted as HH:mm on a 24-hour clock, for example 21:15.");
            }

            // Asked of the queue, a clock time is a moment ahead: the next time the clock reads
            // it. One that has already passed today therefore means tomorrow.
            DateTime today = DateOnly.FromDateTime(now).ToDateTime(parsedTime);
            at = today >= now ? today : today.AddDays(1);
        }

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.GetQueueAsync(limit, now, trackFilter, at, PointInTimeWindowSeconds, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("What has already played, newest first, plus what is playing now. Returns serverTime, so you never have to guess the current date or time. There are six ways to call it: with no parameters for the most recent tracks; with minutesAgo for 'what was that ten minutes ago'; with time, plus date for a day other than today, for 'what was playing around 16:45'; with date alone for 'what did we play on Monday'; with trackIds for 'when did we last hear it'; or with personId for 'when did we last hear something by Toto'. The minutesAgo and time forms ask about a moment, and return context either side of it. A clock time is matched as the minute that follows it. For a point in time, exactly one entry carries bestMatch true: the play covering most of that minute, or the nearest play if nothing was on, in which case hint says so. overlapsTargetTime is set on every entry sounding during that minute, often two when a track boundary falls inside it. precededBy and followedBy are the plays either side of the match: name all three when you answer, because the listener's time is usually approximate and the one they meant is often a neighbour rather than the match. targetTime echoes the moment actually used. entries are plays that have finished; the track still playing is reported separately as nowPlaying and is not repeated, except when a point-in-time lookup lands inside it, where stillPlaying marks it and its endedAt is a projection rather than a fact. An entry with hidden true is a real play on an album you may not see: its times are accurate but its title and artist are withheld, so the timeline has no unexplained gaps. nowPlaying can be hidden too, which means something is playing that you cannot see, not that the stream is idle. Hidden entries carry a note field explaining them in words: relay it rather than omitting the entry. hint explains an empty result.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<HistoryView> GetHistory(
        ClaimsPrincipal user,
        [Description("How many past entries to return (1-100). For a point in time, they are split either side of it.")] int limit = DefaultPlaybackLimit,
        [Description("Optional date as yyyy-MM-dd, in the server's local time. Defaults to today when a time is given. Resolve words like 'yesterday' or 'Monday' against the serverTime from the most recent tool response rather than guessing, and rather than one you read earlier in the conversation. Date alone returns the end of that day's playback, so for part of a day, such as 'yesterday afternoon', pass a time in that part as well, such as 15:00.")] string date = null,
        [Description("Optional clock time as HH:mm, 24-hour, server-local. Returns whatever was playing at that moment on the given date. If the listener's time could be read two ways, as '9:15' can, use whichever was most recent, and say which you used so they can correct it.")] string time = null,
        [Description("Optional shortcut for a relative question: how many minutes before now to look. Takes precedence over date and time. Use 0 or omit when not asking about a relative moment.")] int minutesAgo = 0,
        [Description("Optional: track IDs from earlier results, for 'when did we last hear it'. Pass every version of a song — album edit, radio edit, remix — to ask about the song rather than one recording. Different songs that share a title, by different artists, are separate lookups: one call per song. Returns the page centred on the most recent play of any of them, flagged bestMatch, including a play still in progress, marked stillPlaying. earlierPlay is the play before that of any of them, whichever version it was. If none has played, entries is empty and hint says so. Takes precedence over minutesAgo, time and date. At most 100.")] List<int> trackIds = null,
        [Description("Optional: a person or group ID from playlist_tools__search_people, for 'when did we last hear something by Toto'. Returns the page centred on the most recent play of anything credited to them, at track or album level, flagged bestMatch, with earlierPlay the play before that. For two people who share a name, look each up separately. Takes precedence over minutesAgo, time and date; trackIds takes precedence over it. Use 0 or omit otherwise.")] int personId = 0,
        [Description("Optional role filter for personId, for 'composed by' rather than any credit. Valid values come from playlist_tools__get_roles. Only meaningful together with personId.")] string role = null)
    {
        limit = Math.Clamp(limit, 1, MaxResultLimit);

        DateTime now = systemClock.Now;
        DateTime? at = null;
        DateOnly? day = null;
        List<int> trackFilter = NormalizeTrackIds(trackIds);

        if (!string.IsNullOrWhiteSpace(role) && personId <= 0)
        {
            throw new McpException("The 'role' filter only works together with 'personId'. To filter by a named person, first call playlist_tools__search_people to resolve them to a personId, then pass that personId here.");
        }

        int? personFilter = trackFilter is null && personId > 0 ? personId : null;

        // A lookup by track or by person is a different question from one about a moment or a
        // day, so those parameters are ignored rather than combined with it.
        bool lookup = trackFilter != null || personFilter != null;

        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed))
            {
                throw new McpException("The 'date' parameter must be formatted as yyyy-MM-dd, for example 2026-09-08.");
            }

            parsedDate = parsed;
        }

        if (!lookup && minutesAgo > 0)
        {
            at = now.AddMinutes(-minutesAgo);
        }
        else if (!lookup && !string.IsNullOrWhiteSpace(time))
        {
            if (!TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
            {
                throw new McpException("The 'time' parameter must be formatted as HH:mm on a 24-hour clock, for example 16:45.");
            }

            // A bare time means today unless a date says otherwise.
            DateOnly onDate = parsedDate ?? DateOnly.FromDateTime(now);
            at = onDate.ToDateTime(parsedTime);
        }
        else if (!lookup && parsedDate.HasValue)
        {
            day = parsedDate;
        }

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.GetHistoryAsync(limit, now, trackFilter, personFilter, personFilter.HasValue ? role : null, at, PointInTimeWindowSeconds, day, allowOnlyPublicAlbums);
    }

    // The stream's repeat rules, as both picking and adding apply them.
    private static PickRules RepeatRules() => new(MinutesBetweenTrackRepeat, MinutesBetweenAlbumRepeat, UseWeightedPicks);

    /// <summary>
    /// The track IDs a lookup should use: positive, without duplicates, at most the result
    /// ceiling so the database query stays inside every provider's parameter limits. Null when
    /// none are usable, which means "no track lookup" rather than "look up nothing".
    /// </summary>
    private static List<int> NormalizeTrackIds(List<int> trackIds)
    {
        if (trackIds == null)
        {
            return null;
        }

        List<int> usable = trackIds.Where(id => id > 0).Distinct().ToList();

        if (usable.Count > MaxResultLimit)
        {
            throw new McpException($"Pass at most {MaxResultLimit} track IDs. For more than that, look the tracks up in smaller groups.");
        }

        return usable.Count == 0 ? null : usable;
    }

    // Mutating tool: appends to shared queue state. Hints are set explicitly so clients can gate/approve it.
    // Destructive defaults to true; appending twice adds twice, so it is not idempotent.
    [McpServerTool(Destructive = true, Idempotent = false), Description("Append tracks to the global stream queue that every listener hears, or insert at Position, shifting the rest back. Only call this when the user's latest message asks for something to be played or queued. A question about the queue is answered with the read tools, never by adding, and a track that has left the queue was removed on purpose or has already played, so never put it back unless asked. Unless this connection may set the stream's repeat rules aside, a track that played recently or is already queued, or one whose album played within the last hour, is skipped, and skipped says why: tell the listener rather than queueing something else in its place. Returns the IDs that landed, the resulting queueLength (tracks waiting behind the one playing, which is not counted), and a skipped list for IDs that could not be queued — these do not fail the call, so always check skipped and tell the user what did not make it. firstAddedPosition is where the first added track landed, counting from 1 at the front of the queue, and the hint says in words when it will play — use them instead of describing the position yourself. Adding is permanent: nothing can remove, reorder or empty the queue, so never offer to undo or change an add.")]
    [RequirePermission(CorePermissions.PlaylistEdit)]
    public async Task<QueueAddResult> AddToQueue(
        ClaimsPrincipal user,
        [Description("Array of integer track IDs to enqueue, e.g. [123, 456]. Obtain these from playlist_tools__search_tracks, playlist_tools__pick_tracks, playlist_tools__get_album_tracklist, or the entries of playlist_tools__get_queue and playlist_tools__get_history. Unplayable tracks are skipped, and listed in skipped.")] List<int> trackIds,
        [Description("Where to insert in the queue: omit or -1 = end of queue; 0 = start of queue, so it plays next. Use 0 only when the user asked for it next: 'play X next', 'after this one'. A word inside the title is never that instruction: 'play The Next Episode' is a plain request, so omit position.")] int position = -1,
        [Description("If true, stop whatever is playing and start the tracks being added, cutting off the current track for everyone listening. Only set this when the user explicitly asked for immediacy: now, immediately, right now. A word inside the title is never that instruction: if the request is just 'play' and a title — 'play Now We Are Free', 'play Right Here Right Now' — this is false, even though the request contains 'now'. 'Play X', 'add X', 'queue X' and 'can we hear X' are all requests to append, not to interrupt. Because it advances the stream to the next queue entry it forces insertion at the front: use it with position 0 or with position omitted, never with a position further down the queue.")] bool playNow = false,
        [Description("Set true only after the user has been told how many tracks this will add and has answered that they want it. Their answer, not your assumption: a cancelled, rejected or unanswered question is not agreement, and nor is an option you suggested yourself. Required above 25 tracks, which the tool refuses without it; leave false otherwise.")] bool confirmed = false)
    {
        if (trackIds == null || trackIds.Count == 0)
        {
            throw new McpException("trackIds parameter is required, and must be non-empty.");
        }

        if (playNow && position > 0)
        {
            throw new McpException("playNow only plays the tracks you are adding when they go to the front of the queue. Use position 0, or omit position, together with playNow.");
        }

        if (trackIds.Count > ConfirmLargeAddThreshold && !confirmed)
        {
            throw new McpException(
                $"This would add {trackIds.Count} tracks to a queue everyone is listening to, which needs the user's agreement first. Tell them it is {trackIds.Count} tracks and ask whether to go ahead, with a tool for asking if you have one. Do not call this again until they have actually answered; deciding for them is not agreement. When they agree, call this again with the same track IDs and confirmed set to true.");
        }

        int? positionFilter = position >= 0 ? position : null;

        logger.LogInformation("AddToQueue called with trackIDs {trackIds} for position {position} with PlayNow: {playNow}", string.Join(", ", trackIds), position, playNow);

        bool allowIgnoringRules = await permissionAuthorizer.HasAnyAsync(user, CorePermissions.PlaylistRulesIgnore);
        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        QueueAddResult result = await musicSearchService.AddTracksToQueueAsync(trackIds, RepeatRules(), systemClock.Now, positionFilter, playNow, allowIgnoringRules, allowOnlyPublicAlbums);

        // Carried in the response rather than left to the system prompt, which is routinely
        // dropped by the time a multi-track add completes.
        if (result.AddedTrackIds.Count > AnnounceAddThreshold)
        {
            result = result with { Hint = $"This added {result.AddedTrackIds.Count} tracks. Say how many when you confirm it to the user. {result.Hint}".Trim() };
        }

        return result;
    }
}