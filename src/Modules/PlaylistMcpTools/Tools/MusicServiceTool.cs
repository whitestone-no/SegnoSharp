using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

    // A requested clock time is accurate to the minute, so it is matched as a minute-long
    // window and the play covering most of it wins, rather than whatever happened to be on
    // at second zero.
    private const int HistoryWindowSeconds = 60;

    // A large add monopolises a shared stream, so past this many tracks the call is refused
    // until the caller has told the user the size and been told to go ahead. Past the smaller
    // threshold it succeeds but is told to mention the count, because a rule carried back in
    // the response is followed far more often than one in a system prompt.
    private const int ConfirmLargeAddThreshold = 25;
    private const int AnnounceAddThreshold = 10;

    [McpServerTool(ReadOnly = true), Description("List every credit role that can be passed as a 'role' filter, with the scopes (Album and/or Track) it applies to. Call this first if you are unsure which role values are valid, never pass a role string that did not come from here, and do not assume the list is fixed.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<RoleResult>> GetRoles()
    {
        return await musicSearchService.GetRolesAsync();
    }

    [McpServerTool(ReadOnly = true), Description("Resolve a person/group name to candidates with per-role credit counts and sample works. Use creditCounts and sampleWorks to tell same-named people apart (the film composer versus the guitarist); a number in parentheses after a name marks a second person with that name.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<PersonResult>> SearchPeople(
        ClaimsPrincipal user,
        [Description("Free-text name of the person or group to search for, e.g. 'Beethoven' or 'The Beatles'.")] string query,
        [Description("Optional role filter. Candidates with no credit in this role are excluded, and the returned creditCounts and sampleWorks are narrowed to it. Valid values come from playlist_tools__get_roles. Call playlist_tools__get_roles if unsure. Do not invent other values.")] string role = null,
        [Description("Maximum number of candidate people/groups to return (1-25).")] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new McpException("The query parameter is required.");
        }

        limit = Math.Clamp(limit, 1, 25);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.SearchPeopleAsync(query, role, limit, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("Resolve albums by title, by credited person, or both. Returns album-level credits, a match score, and totalMatches: the number of albums that matched before the limit was applied. When totalMatches is larger than the list you got back, you are holding a slice, not the whole set, so raise limit or say so. If truncated is true, a title search matched more albums than the ranking looks at, so the results came from a subset: search a more specific title or add a personId rather than trusting the order. At least one of query and personId is required. Use personId on its own to answer 'what else do we have by X'; do not group the results of a track search to answer that, because a track search stops at its own limit and will under-report. Prefer the plainest exact title match: sequels ('... II') and qualified editions ('... (Complete Rejected Score)', deluxe, live) should only be chosen when the user asked for them.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<AlbumSearchResult> SearchAlbums(
        ClaimsPrincipal user,
        [Description("Optional free-text album title to search for. Leave empty to match on personId only.")] string query = null,
        [Description("Optional person/group ID to filter by, as returned by playlist_tools__search_people. Matches albums credited to them at album level, and albums carrying a track credited to them. Use 0 or omit to not filter by person.")] int personId = 0,
        [Description("Optional role filter. Valid values come from playlist_tools__get_roles. Call playlist_tools__get_roles if unsure. Do not invent other values. Only meaningful together with personId.")] string role = null,
        [Description("Maximum number of albums to return (1-100). Compare it against totalMatches before calling a list complete. A person search is exact at any size; a title search ranks a capped candidate pool, so values much above 100 gain nothing there.")] int limit = 5)
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

        limit = Math.Clamp(limit, 1, 100);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.SearchAlbumsAsync(query, personIdFilter, role, limit, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("Search playable tracks by any combination of title, person/group, role and album. Each result carries its album, year, length and full credits, so no extra lookup is needed to describe it. At least one of titleQuery, personId or albumId is required. Title matching ranks a capped set of candidates, so a title made of common words can crowd out the right track: resolve the person with playlist_tools__search_people or the album with playlist_tools__search_albums first and pass personId or albumId whenever you can, and narrow further if the result comes back with truncated true. The outcome field: Matched means at least one candidate reached minScore and is returned; WeakMatch means candidates exist but none reached minScore, so nothing is returned and topScore plus hint say how close the best one came; NoMatch means nothing exists in this scope. Read hint for the next step.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<TrackSearchResult> SearchTracks(
        ClaimsPrincipal user,
        [Description("Optional free-text track title to match. Leave empty to match on the other filters only.")] string titleQuery = null,
        [Description("Optional person/group ID to filter by, as returned by playlist_tools__search_people. Use 0 or omit to not filter by person.")] int personId = 0,
        [Description("Optional role filter. Valid values come from playlist_tools__get_roles. Call playlist_tools__get_roles if unsure. Do not invent other values. Only meaningful together with personId.")] string role = null,
        [Description("Optional album ID to filter by, as returned by playlist_tools__search_albums. Use 0 or omit to not filter by album.")] int albumId = 0,
        [Description("Maximum number of tracks to return (1-100).")] int limit = 10,
        [Description("Minimum fuzzy-match score in the range 0.0-1.0. Candidates below it are excluded, so raising it narrows the results and lowering it widens them. Leave at the default for loose phrasing; raise it to about 0.7 when you believe you have the exact title. Do not go below 0.3, where matches stop being meaningful.")] double minScore = 0.4)
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

        limit = Math.Clamp(limit, 1, 100);
        minScore = Math.Clamp(minScore, 0.0, 1.0);

        TrackSearchQuery query = new(titleQuery, personIdFilter, role, albumIdFilter, limit);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.SearchTracksAsync(query, minScore, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("Full disc/track tree for one album, in order, including unplayable tracks (isPlayable false), for verifying an externally-suggested title against what actually exists and for queueing a complete album. Never pass a track with isPlayable false to playlist_tools__add_to_queue.")]
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

    [McpServerTool(ReadOnly = true), Description("Pick one or more random tracks credited to a person/group, for open-ended requests like 'play something by X'. Honours the stream's no-repeat rules, so recently played tracks and albums are excluded: it can return fewer tracks than requested, or none at all when everything by that person has played recently. poolSize is how many eligible tracks it chose from. Picks are always playable, and the score on a pick is not a match quality — ignore it.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<TrackPickResult> PickTracks(
        ClaimsPrincipal user,
        [Description("The person/group ID to pick tracks for, as returned by playlist_tools__search_people. Must be a positive ID.")] int personId,
        [Description("Optional role filter. Valid values come from playlist_tools__get_roles. Call playlist_tools__get_roles if unsure. Do not invent other values.")] string role = null,
        [Description("Number of tracks to pick (1-50). Leave at 1 unless the user asked for several: 'play something by X' and 'put on some X' both mean one track. Fewer may come back than requested if the eligible pool is smaller.")] int count = 1)
    {
        if (personId <= 0)
        {
            throw new McpException("personId must be a positive ID, as returned by playlist_tools__search_people.");
        }

        // Clamp to a sane range. The service also floors count at 1, but we enforce both bounds here explicitly.
        count = Math.Clamp(count, 1, 50);

        PickRules rules = new(
            MinutesBetweenTrackRepeat,
            MinutesBetweenAlbumRepeat,
            UseWeightedPicks);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.PickTracksAsync(
            personId,
            rules,
            systemClock.Now,
            randomGenerator.GetInt,
            role,
            count,
            allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("What is playing right now and what is queued behind it. Returns serverTime (the clock everything here is relative to, and your only source of the current time), nowPlaying with how many seconds are elapsed and remaining, the next entries with their position and estimated start time, and queueLength for the whole queue. Use this for 'what is this', 'what is next', 'what is coming up'. estimatedStart assumes back-to-back playback: treat it as reliable for the next few entries and as a rough guide further out. Nothing records who or what added an entry, so never say a track was requested by anyone, not even one you queued yourself. An entry with hidden true is a real track on an album you may not see: it keeps its position and timing but its title and artist are withheld. Positions are always contiguous, so a hidden entry is never a gap or an error. Its note field explains this in words: relay that, never guess what the track is, and never leave the entry out when listing what is coming up.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<QueueView> GetQueue(
        ClaimsPrincipal user,
        [Description("How many upcoming entries to return (1-100). Compare against queueLength before calling the list complete.")] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 100);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.GetQueueAsync(limit, systemClock.Now, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("What has already played, newest first, plus what is playing now. Returns serverTime, so you never have to guess the current date or time. With no parameters it returns the most recent tracks. Give minutesAgo for 'what was that ten minutes ago', or time (with date for a day other than today) for 'what was playing around 16:45' — either returns the track that was playing around that moment plus context either side. The time is matched as the minute that follows it. Exactly one entry carries bestMatch true: the play covering most of that minute, or the nearest play if nothing was on, in which case hint says so. overlapsRequestedTime is set on every entry sounding during that minute, which is often two when a track boundary falls inside it. Give date alone for 'what did we play on Monday'. entries are plays that have finished, newest first; the track still playing is reported separately as nowPlaying and is not repeated here, except when a point-in-time lookup lands inside it, where stillPlaying marks it and its endedAt is a projection rather than a fact. An entry with hidden true is a real play on an album you may not see: its times are accurate but its title and artist are withheld, so the timeline has no unexplained gaps. nowPlaying can be hidden too, which means something is playing that you cannot see, not that the stream is idle. Hidden entries carry a note field explaining them in words: relay it rather than omitting the entry. resolvedAt echoes the moment actually used, and hint explains an empty result.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<HistoryView> GetHistory(
        ClaimsPrincipal user,
        [Description("How many past entries to return (1-100). For a point in time, they are split either side of it.")] int limit = 10,
        [Description("Optional date as yyyy-MM-dd, in the server's local time. Defaults to today when a time is given. Resolve words like 'yesterday' or 'Monday' against serverTime from an earlier call rather than guessing.")] string date = null,
        [Description("Optional clock time as HH:mm, 24-hour, server-local. Returns whatever was playing at that moment on the given date.")] string time = null,
        [Description("Optional shortcut for a relative question: how many minutes before now to look. Takes precedence over date and time. Use 0 or omit when not asking about a relative moment.")] int minutesAgo = 0)
    {
        limit = Math.Clamp(limit, 1, 100);

        DateTime now = systemClock.Now;
        DateTime? at = null;
        DateOnly? day = null;

        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed))
            {
                throw new McpException("The 'date' parameter must be formatted as yyyy-MM-dd, for example 2026-09-08.");
            }

            parsedDate = parsed;
        }

        if (minutesAgo > 0)
        {
            at = now.AddMinutes(-minutesAgo);
        }
        else if (!string.IsNullOrWhiteSpace(time))
        {
            if (!TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
            {
                throw new McpException("The 'time' parameter must be formatted as HH:mm on a 24-hour clock, for example 16:45.");
            }

            // A bare time means today unless a date says otherwise.
            DateOnly onDate = parsedDate ?? DateOnly.FromDateTime(now);
            at = onDate.ToDateTime(parsedTime);
        }
        else if (parsedDate.HasValue)
        {
            day = parsedDate;
        }

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.GetHistoryAsync(limit, now, at, day, HistoryWindowSeconds, allowOnlyPublicAlbums);
    }

    // Mutating tool: appends to shared queue state. Hints are set explicitly so clients can gate/approve it.
    // Destructive defaults to true; appending twice adds twice, so it is not idempotent.
    [McpServerTool(Destructive = true, Idempotent = false), Description("Append tracks to the global stream queue that every listener hears, or insert at Position, shifting the rest back. Returns the IDs that landed, the resulting queueLength, and a skipped list for IDs that could not be queued — these do not fail the call, so always check skipped and tell the user what did not make it. The queue cannot be listed, reordered or emptied through these tools.")]
    [RequirePermission(CorePermissions.PlaylistEdit)]
    public async Task<QueueAddResult> AddToQueue(
        ClaimsPrincipal user,
        [Description("Array of integer track IDs to enqueue, e.g. [123, 456]. Obtain these from playlist_tools__search_tracks, playlist_tools__pick_tracks or playlist_tools__get_album_tracklist.")] List<int> trackIds,
        [Description("Where to insert in the queue: omit or -1 = end of queue; 0 = start of queue.")] int position = -1,
        [Description("If true, stop whatever is playing and start the tracks being added, cutting off the current track for everyone listening. Only set this when the user explicitly asked for immediacy: now, immediately, right now, put it on. 'Play X', 'add X', 'queue X' and 'can we hear X' are all requests to append, not to interrupt. Because it advances the stream to the next queue entry it forces insertion at the front: use it with position 0 or with position omitted, never with a position further down the queue.")] bool playNow = false,
        [Description("Set true only after the user has been told how many tracks this will add and has answered that they want it. Their answer, not your assumption. Required for large additions; leave false otherwise.")] bool confirmed = false)
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
                $"This would add {trackIds.Count} tracks to a queue everyone is listening to, which needs the user's agreement first. Tell them it is {trackIds.Count} tracks and ask whether to go ahead, using an ask-user tool if you have one. Do not call this again until they have actually answered; deciding for them is not agreement. When they agree, look the tracks up again and call this with confirmed set to true.");
        }

        int? positionFilter = position >= 0 ? position : null;

        logger.LogInformation("AddToQueue called with trackIDs {trackIds} for position {position} with PlayNow: {playNow}", string.Join(", ", trackIds), position, playNow);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        QueueAddResult result = await musicSearchService.AddTracksToQueueAsync(trackIds, positionFilter, playNow, allowOnlyPublicAlbums);

        // Carried in the response rather than left to the system prompt, which is routinely
        // dropped by the time a multi-track add completes.
        if (result.AddedTrackIds.Count > AnnounceAddThreshold)
        {
            result = result with { Note = $"This added {result.AddedTrackIds.Count} tracks. Say how many when you confirm it to the user." };
        }

        return result;
    }
}