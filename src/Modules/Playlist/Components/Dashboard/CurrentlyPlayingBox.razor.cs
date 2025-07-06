using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Modules.Playlist.ViewModels;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Dashboard
{
    public partial class CurrentlyPlayingBox : IDashboardBox, IAsyncEventHandler<PlaylistUpdated>
    {
        public static string Name => "Currently playing";
        public static string Title => "Currently playing";
        public static string AdditionalCss => $"_moduleresource/{typeof(CurrentlyPlayingBox).Assembly.GetName().Name}/dashboard.css";

        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private ISystemClock SystemClock { get; set; }
        [Inject] private IHashingUtil HashingUtil { get; set; }
        [Inject] private ILogger<CurrentlyPlayingBox> Logger { get; set; }

        private PlaylistViewModel CurrentlyPlaying { get; set; }

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

                CurrentlyPlaying = await dbContext.StreamHistory
                    .AsNoTracking()
                    .Where(h => h.Played.AddSeconds(h.TrackStreamInfo.Track.Length) > now)
                    .OrderByDescending(h => h.Played)
                    .Select(h => new PlaylistViewModel
                    {

                        AlbumTitle = h.TrackStreamInfo.Track.Disc.Album.Title,
                        TrackTitle = h.TrackStreamInfo.Track.Title,
                        Length = h.TrackStreamInfo.Track.Duration,
                        QueueId = h.Id,
                        AlbumId = h.TrackStreamInfo.Track.Disc.Album.Id,
                        HasAlbumCover = h.TrackStreamInfo.Track.Disc.Album.AlbumCover != null,
                        TrackArtists = string.Join(", ",
                            h.TrackStreamInfo.Track.TrackPersonGroupPersonRelations
                                .Where(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                                .SelectMany(r =>
                                    r.Persons.Select(p =>
                                        p.FirstName == null ? p.LastName : p.FirstName + " " + p.LastName))),
                        AlbumArtists = string.Join(", ",
                            h.TrackStreamInfo.Track.Disc.Album.AlbumPersonGroupPersonRelations
                                .Where(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                                .SelectMany(r =>
                                    r.Persons.Select(p =>
                                        p.FirstName == null ? p.LastName : p.FirstName + " " + p.LastName)))
                    })
                    .FirstOrDefaultAsync();

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "{exceptionMessage}", e.Message);
            }
        }
    }
}
