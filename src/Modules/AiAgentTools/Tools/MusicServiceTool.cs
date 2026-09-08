using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Claims;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Models;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Services;
using Whitestone.SegnoSharp.Shared.Attributes.Security;
using Whitestone.SegnoSharp.Shared.Helpers.Security;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Permissions;
// ReSharper disable UnusedMember.Global - ReSharper reports that methods are not use, but these methods are just exposed externally, never called by SegnoSharp itself.

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Tools;

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

    [McpServerTool(ReadOnly = true), Description("List every credit role that can be passed as a 'role' filter, with the scopes (Album and/or Track) it applies to. Call this first if you are unsure which role values are valid, never pass a role string that did not come from here, and do not assume the list is fixed.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<RoleResult>> GetRoles()
    {
        return await musicSearchService.GetRolesAsync();
    }

    [McpServerTool(ReadOnly = true), Description("Resolve a person/group name to candidates with per-role credit counts and sample works. Use CreditCounts and SampleWorks to tell same-named people apart (the film composer versus the guitarist); a number in parentheses after a name marks a second person with that name.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<PersonResult>> SearchPeople(
        ClaimsPrincipal user,
        [Description("Free-text name of the person or group to search for, e.g. 'Beethoven' or 'The Beatles'.")] string query,
        [Description("Optional role filter. Candidates with no credit in this role are excluded, and the returned CreditCounts and SampleWorks are narrowed to it. Valid values come from GetRoles. Call GetRoles if unsure. Do not invent other values.")] string role = null,
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

    [McpServerTool(ReadOnly = true), Description("Resolve an album title to candidates with album-level credits and a match score. Prefer the plainest exact title match: sequels ('... II') and qualified editions ('... (Complete Rejected Score)', deluxe, live) should only be chosen when the user asked for them.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<AlbumResult>> SearchAlbums(
        ClaimsPrincipal user,
        [Description("Free-text album title to search for.")] string query,
        [Description("Maximum number of candidate albums to return (1-25).")] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new McpException("The query parameter is required.");
        }

        limit = Math.Clamp(limit, 1, 25);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.SearchAlbumsAsync(query, limit, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("Search playable tracks by any combination of title, person/group, role and album. Each result carries its album, year, length and full credits, so no extra lookup is needed to describe it. At least one of titleQuery, personId or albumId is required. Title matching ranks a capped set of candidates, so a title made of common words can crowd out the right track: resolve the person with SearchPeople or the album with SearchAlbums first and pass personId or albumId whenever you can, and narrow further if the result comes back with Truncated true. Outcome: Matched means at least one candidate reached minScore and is returned; WeakMatch means candidates exist but none reached minScore, so nothing is returned and TopScore plus Hint say how close the best one came; NoMatch means nothing exists in this scope. Read Hint for the next step.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<TrackSearchResult> SearchTracks(
        ClaimsPrincipal user,
        [Description("Optional free-text track title to match. Leave empty to match on the other filters only.")] string titleQuery = null,
        [Description("Optional person/group ID to filter by, as returned by SearchPeople. Use 0 or omit to not filter by person.")] int personId = 0,
        [Description("Optional role filter. Valid values come from GetRoles. Call GetRoles if unsure. Do not invent other values. Only meaningful together with personId.")] string role = null,
        [Description("Optional album ID to filter by, as returned by SearchAlbums. Use 0 or omit to not filter by album.")] int albumId = 0,
        [Description("Maximum number of tracks to return (1-100).")] int limit = 10,
        [Description("Minimum fuzzy-match score in the range 0.0-1.0. Candidates below it are excluded, so raising it narrows the results and lowering it widens them. Leave at the default for loose phrasing; raise it to about 0.7 when you believe you have the exact title. Do not go below 0.3, where matches stop being meaningful.")] double minScore = 0.4)
    {
        // Translate the MCP-facing sentinels (0 = not supplied) to the service's nullable contract.
        int? personIdFilter = personId > 0 ? personId : null;
        int? albumIdFilter = albumId > 0 ? albumId : null;

        if (!string.IsNullOrWhiteSpace(role) && personIdFilter is null)
        {
            throw new McpException("The 'role' filter only works together with 'personId'. To filter by a named artist, first call SearchPeople to resolve them to a personId, then pass that personId here.");
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

    [McpServerTool(ReadOnly = true), Description("Full disc/track tree for one album, in order, including unplayable tracks (IsPlayable false), for verifying an externally-suggested title against what actually exists and for queueing a complete album. Never pass a track with IsPlayable false to AddToQueue.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<AlbumTracklist> GetAlbumTracklist(
        ClaimsPrincipal user,
        [Description("The album ID whose full disc/track tree to retrieve, as returned by SearchAlbums or SearchTracks.")] int albumId)
    {
        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        AlbumTracklist tracklist = await musicSearchService.GetAlbumTracklistAsync(albumId, allowOnlyPublicAlbums);

        if (tracklist == null)
        {
            throw new McpException("No album with that ID is available. It may not exist, or may not be visible to you. Search for the album again rather than trying other IDs.");
        }

        return tracklist;
    }

    [McpServerTool(ReadOnly = true), Description("Pick one or more random tracks credited to a person/group, for open-ended requests like 'play something by X'. Honours the stream's no-repeat rules, so recently played tracks and albums are excluded: it can return fewer tracks than requested, or none at all when everything by that person has played recently. PoolSize is how many eligible tracks it chose from. Picks are always playable, and the Score on a pick is not a match quality — ignore it.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<TrackPickResult> PickTracks(
        ClaimsPrincipal user,
        [Description("The person/group ID to pick tracks for, as returned by SearchPeople. Must be a positive ID.")] int personId,
        [Description("Optional role filter. Valid values come from GetRoles. Call GetRoles if unsure. Do not invent other values.")] string role = null,
        [Description("Number of tracks to pick (1-50). Fewer may come back if the eligible pool is smaller.")] int count = 1)
    {
        if (personId <= 0)
        {
            throw new McpException("personId must be a positive ID, as returned by SearchPeople.");
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

    // Mutating tool: appends to shared queue state. Hints are set explicitly so clients can gate/approve it.
    // Destructive defaults to true; appending twice adds twice, so it is not idempotent.
    [McpServerTool(Destructive = true, Idempotent = false), Description("Append tracks to the global stream queue that every listener hears, or insert at Position, shifting the rest back. Returns the IDs that landed, the resulting QueueLength, and a Skipped list for IDs that could not be queued — these do not fail the call, so always check Skipped and tell the user what did not make it. The queue cannot be listed, reordered or emptied through these tools.")]
    [RequirePermission(CorePermissions.PlaylistEdit)]
    public async Task<QueueAddResult> AddToQueue(
        ClaimsPrincipal user,
        [Description("Array of integer track IDs to enqueue, e.g. [123, 456]. Obtain these from SearchTracks, PickTracks or GetAlbumTracklist.")] List<int> trackIds,
        [Description("Where to insert in the queue: omit or -1 = end of queue; 0 = start of queue.")] int position = -1,
        [Description("If true, stop whatever is playing and start the tracks being added. Because it advances the stream to the next queue entry, it forces insertion at the front: use it with position 0 or with position omitted, never with a position further down the queue. It interrupts other listeners.")] bool playNow = false)
    {
        if (trackIds == null || trackIds.Count == 0)
        {
            throw new McpException("trackIds parameter is required, and must be non-empty.");
        }

        if (playNow && position > 0)
        {
            throw new McpException("playNow only plays the tracks you are adding when they go to the front of the queue. Use position 0, or omit position, together with playNow.");
        }

        int? positionFilter = position >= 0 ? position : null;

        logger.LogInformation("AddToQueue called with trackIDs {trackIds} for position {position} with PlayNow: {playNow}", string.Join(", ", trackIds), position, playNow);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.AddTracksToQueueAsync(trackIds, positionFilter, playNow, allowOnlyPublicAlbums);
    }
}