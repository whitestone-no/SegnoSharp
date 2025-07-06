using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Modules.Playlist.ViewModels;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Dashboard
{
    public partial class PlaylistBox : IDashboardBox, IAsyncEventHandler<PlaylistUpdated>
    {
        public static string Name => "Playlist";
        public static string Title => "Playlist";
        public static string AdditionalCss => $"_moduleresource/{typeof(CurrentlyPlayingBox).Assembly.GetName().Name}/dashboard.css";

        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private ISystemClock SystemClock { get; set; }
        [Inject] private IHashingUtil HashingUtil { get; set; }
        [Inject] private ILogger<CurrentlyPlayingBox> Logger { get; set; }

        private List<PlaylistViewModel> Playlist { get; set; }
        protected override async Task OnInitializedAsync()
        {
            Cambion.Register(this);

            await HandleEventAsync(new PlaylistUpdated());
        }

        public async Task HandleEventAsync(PlaylistUpdated input)
        {
            try
            {
                SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

                DateTime now = SystemClock.Now;

                Playlist = await dbContext.StreamQueue
                    .AsNoTracking()
                    .OrderBy(q => q.SortOrder)
                    .Select(q => new PlaylistViewModel
                    {

                        AlbumTitle = q.TrackStreamInfo.Track.Disc.Album.Title,
                        TrackTitle = q.TrackStreamInfo.Track.Title,
                        Length = q.TrackStreamInfo.Track.Duration,
                        QueueId = q.Id,
                        AlbumId = q.TrackStreamInfo.Track.Disc.Album.Id,
                        HasAlbumCover = q.TrackStreamInfo.Track.Disc.Album.AlbumCover != null,
                        TrackArtists = string.Join(", ",
                            q.TrackStreamInfo.Track.TrackPersonGroupPersonRelations
                                .Where(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                                .SelectMany(r =>
                                    r.Persons.Select(p =>
                                        p.FirstName == null ? p.LastName : p.FirstName + " " + p.LastName))),
                        AlbumArtists = string.Join(", ",
                            q.TrackStreamInfo.Track.Disc.Album.AlbumPersonGroupPersonRelations
                                .Where(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                                .SelectMany(r =>
                                    r.Persons.Select(p =>
                                        p.FirstName == null ? p.LastName : p.FirstName + " " + p.LastName)))
                    })
                    .Take(3)
                    .ToListAsync();

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "{exceptionMessage}", e.Message);
            }
        }
    }
}
