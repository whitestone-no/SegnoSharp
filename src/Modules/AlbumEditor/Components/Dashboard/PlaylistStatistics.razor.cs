using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.AlbumEditor.ViewModels;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Modules.AlbumEditor.Components.Dashboard
{
    public partial class PlaylistStatistics : IDashboardBox
    {
        public static string Name => "Playlist statistics";
        public static string Title => "Playlist Statistics";
        public static string AdditionalCss => $"_moduleresource/{typeof(PlaylistStatistics).Assembly.GetName().Name}/dashboard.css";

        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private ILogger<PlaylistStatistics> Logger { get; set; }

        private DashboardStatisticsViewModel Statistics { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();
                
                IQueryable<TrackStreamInfo> baseQuery = dbContext.TrackStreamInfos
                    .AsNoTracking()
                    .Where(tsi => tsi.IncludeInAutoPlaylist);

                IQueryable<int> trackArtistIdsQuery = baseQuery
                    .SelectMany(tsi => tsi.Track.TrackPersonGroupPersonRelations
                        .Where(r =>
                            r.PersonGroup.PersonGroupStreamInfo != null &&
                            r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                        .SelectMany(r => r.Persons.Select(p => p.Id)));

                IQueryable<int> albumArtistIdsQuery = baseQuery
                    .SelectMany(tsi => tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations
                        .Where(r =>
                            r.PersonGroup.PersonGroupStreamInfo != null &&
                            r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                        .SelectMany(r => r.Persons.Select(p => p.Id)));
                
                int albumsResult = await baseQuery
                    .Select(tsi => tsi.Track.Disc.Album.Id)
                    .Distinct()
                    .CountAsync();

                int tracksResult = await baseQuery
                    .Select(tsi => tsi.Track.Id)
                    .Distinct()
                    .CountAsync();

                int durationResult = await baseQuery
                    .Select(tsi => tsi.Track)
                    .Distinct()
                    .SumAsync(t => t.Length);

                int artistsResult = await trackArtistIdsQuery
                    .Union(albumArtistIdsQuery)
                    .Distinct()
                    .CountAsync();

                Statistics.NumberOfAlbums = albumsResult;
                Statistics.NumberOfTracks = tracksResult;
                Statistics.TotalDuration = durationResult;
                Statistics.NumberOfArtists = artistsResult;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error when initializing AlbumsStatistics: {exceptionMessage}", e.Message);
            }
        }
    }
}
