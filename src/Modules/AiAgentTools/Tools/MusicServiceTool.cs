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
    [McpServerTool(ReadOnly = true), Description("List every credit role that can be passed as a 'role' filter, with the scope (Album or Track) it applies to. The same role name can appear more than once with different scopes; treat it as a single value. Never pass a role string that did not come from here, and do not assume the list is fixed.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<RoleResult>> GetRoles()
    {
        return await musicSearchService.GetRolesAsync();
    }

    [McpServerTool(ReadOnly = true), Description("Resolve a person or group name to candidate IDs, with per-role credit counts and sample works. Use CreditCounts and SampleWorks to tell same-named people apart (the film composer versus the guitarist); a number in parentheses after a name marks a second person with that name.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<IReadOnlyList<PersonResult>> SearchPeople(
        ClaimsPrincipal user,
        [Description("Free-text name of the person or group to search for, e.g. 'Beethoven' or 'The Beatles'.")] string query,
        [Description("Optional. Narrows the returned CreditCounts and SampleWorks to that role. It does NOT exclude people who lack the role, so check CreditCounts before assuming a candidate matches. Valid values come from GetRoles.")] string role = null,
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

    [McpServerTool(ReadOnly = true), Description("Resolve an album title to candidate IDs, with album-level credits and a match score. Prefer the plainest exact title match: sequels and qualified editions (deluxe, live, rejected score) should only be chosen when the user asked for them.")]
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

    [McpServerTool(ReadOnly = true), Description("Search playable tracks by any combination of title, person/group, role and album. Each result carries its album, year, length and full credits, so no extra lookup is needed to describe it. Always supply at least one filter: an unfiltered call returns arbitrary tracks and reports them as Matched. Title matching examines a capped set of candidates, so a title made of common words can crowd out the right track — resolve the person with SearchPeople or the album with SearchAlbums first and pass personId or albumId whenever you can. Outcome: Matched means the best candidate scored at or above minScore, and a search with no titleQuery is always Matched if anything is credited; WeakMatch means candidates were found but all scored below minScore, so verify before acting; NoMatch means nothing exists in this scope, and Hint says what to try next.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<TrackSearchResult> SearchTracks(
        ClaimsPrincipal user,
        [Description("Optional free-text track title to match. Leave empty to match on the other filters only.")] string titleQuery = null,
        [Description("Optional person/group ID to filter by, as returned by SearchPeople. Use 0 or omit to not filter by person.")] int personId = 0,
        [Description("Optional role filter. Valid values come from GetRoles (currently 'Artist' or 'Composer'). Call GetRoles if unsure. Do not invent other values. Only meaningful together with personId.")] string role = null,
        [Description("Optional album ID to filter by, as returned by SearchAlbums. Use 0 or omit to not filter by album.")] int albumId = 0,
        [Description("Maximum number of tracks to return (1-100).")] int limit = 10,
        [Description("Confidence threshold from 0.0 to 1.0 deciding whether the outcome is reported as Matched or WeakMatch. It does NOT remove tracks from the results. Raise it to around 0.7 when you believe you have the exact title and want a strict verdict; leave it at the default for loose phrasing.")] double minScore = 0.4)
    {
        // Translate the MCP-facing sentinels (0 = not supplied) to the service's nullable contract.
        int? personIdFilter = personId > 0 ? personId : null;
        int? albumIdFilter = albumId > 0 ? albumId : null;

        if (!string.IsNullOrWhiteSpace(role) && personIdFilter is null)
        {
            throw new McpException("The 'role' filter only works together with 'personId'. To filter by a named artist, first call SearchPeople to resolve them to a personId, then pass that personId here.");
        }

        limit = Math.Clamp(limit, 1, 100);
        minScore = Math.Clamp(minScore, 0.0, 1.0);

        TrackSearchQuery query = new(titleQuery, personIdFilter, role, albumIdFilter, limit);

        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        return await musicSearchService.SearchTracksAsync(query, minScore, allowOnlyPublicAlbums);
    }

    [McpServerTool(ReadOnly = true), Description("Full disc and track tree for one album, in order, including unplayable tracks (IsPlayable false). Use it to verify an externally-suggested title against what actually exists, and to enqueue a complete album. Never pass a track with IsPlayable false to AddToQueue.")]
    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<AlbumTracklist> GetAlbumTracklist(
        ClaimsPrincipal user,
        [Description("The album ID whose full disc/track tree to retrieve, as returned by SearchAlbums or SearchTracks.")] int albumId)
    {
        bool allowOnlyPublicAlbums = !await permissionAuthorizer.HasAnyAsync(user, CorePermissions.AlbumsViewAll);

        AlbumTracklist tracklist = await musicSearchService.GetAlbumTracklistAsync(albumId, allowOnlyPublicAlbums);

        if (tracklist == null)
        {
            throw new McpException("No tracks found for the specified album.");
        }

        return tracklist;
    }

    [McpServerTool(ReadOnly = true), Description("Pick one or more random tracks credited to a person or group, for open-ended requests like \"play something by X\". Honours the stream's no-repeat rules, so recently played tracks and albums are excluded: it can return fewer tracks than requested, or none at all when everything by that person has played recently. PoolSize is the number of eligible tracks it chose from. Picks are always playable. The Score on a pick is not a match quality and should be ignored.")]

    [RequirePermission(CorePermissions.AlbumsView, CorePermissions.AlbumsViewAll)]
    public async Task<TrackPickResult> PickTracks(
        ClaimsPrincipal user,
        [Description("The person/group ID to pick tracks for, as returned by SearchPeople. Must be a positive ID.")] int personId,
        [Description("Optional role filter. Valid values come from GetRoles (currently 'Artist' or 'Composer'). Call GetRoles if unsure. Do not invent other values.")] string role = null,
        [Description("Number of tracks to pick (1-50). Fewer may come back if the eligible pool is smaller.")] int count = 1)
    {
        if (personId <= 0)
        {
            throw new McpException("personId must be a positive ID, as returned by SearchPeople.");
        }

        // Clamp to a sane range. The service also floors count at 1, but we enforce both bounds here explicitly.
        count = Math.Clamp(count, 1, 50);

        PickRules rules = new(
            15480,
            60,
            true);

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
    [McpServerTool(Destructive = true, Idempotent = false), Description("Append tracks to the single global stream queue that every listener hears, or insert at Position, shifting the rest back. Returns the IDs that landed, the resulting QueueLength, and a Skipped list for IDs with no playable stream — these do not fail the call, so always check Skipped. The queue cannot be listed, reordered or emptied through these tools.")]
    [RequirePermission(CorePermissions.PlaylistEdit)]
    public async Task<QueueAddResult> AddToQueue(
        [Description("Array of integer track IDs to enqueue, e.g. [123, 456]. Obtain these from SearchTracks or GetAlbumTracklist.")] List<int> trackIds,
        [Description("Where to insert: omit or -1 for the end of the queue; 0 or 1 for the front, shifting everything else back.")] int position = -1,
        [Description("If true, immediately stops whatever is playing and advances to the next track in the queue. This only plays the tracks you just added if you also set position to 0 — with position at the end, it plays whatever was already queued next instead. It interrupts other listeners.")] bool playNow = false)
    {
        if (trackIds == null || trackIds.Count == 0)
        {
            throw new McpException("trackIds parameter is required, and must be non-empty.");
        }

        int? positionFilter = position >= 0 ? position : null;

        logger.LogInformation("AddToQueue called with trackIDs {trackIds} for position {position} with PlayNow: {playNow}", string.Join(", ", trackIds), position, playNow);

        return await musicSearchService.AddTracksToQueueAsync(trackIds, positionFilter, playNow);
    }
}