using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.Playlist.ViewModels;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Pages
{
    public partial class History : IEventHandler<PlaylistUpdated>
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private IHashingUtil HashingUtil { get; set; }
        [Inject] private ISystemClock SystemClock { get; set; }
        [Inject] private ILogger<Playlist> Logger { get; set; }

        private List<HistoryViewModel> HistoryItems { get; set; } = [];
        public DateTime SelectedDate { get; set; }
        public DateTime MinDate { get; set; }

        private const int HistoryPageSize = 10;
        private int HistoryTotalPages { get; set; }
        private int HistoryCurrentPage { get; set; } = 1;


        protected override void OnInitialized()
        {
            SelectedDate = SystemClock.Now;

            Cambion.Register(this);

            HandleEvent(new PlaylistUpdated());
        }

        // ReSharper disable once AsyncVoidMethod
        public async void HandleEvent(PlaylistUpdated input)
        {
            try
            {
                DateTime now = SystemClock.Now;

                if (SelectedDate.Date != now.Date)
                {
                    // Don't need to update the page when a track changes if the user is currently viewing history for a different date
                    return;
                }

                await OnHistoryPageChanged(1);

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "{exceptionMessage}", e.Message);
            }
        }

        private async Task OnDateChanged()
        {
            await OnHistoryPageChanged(1);
        }

        private async Task OnHistoryPageChanged(int page)
        {
            HistoryCurrentPage = page;
            DateTime now = SystemClock.Now;

            SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            HistoryTotalPages = (int)Math.Ceiling(await dbContext.StreamHistory
                .AsNoTracking()
                .Where(h => h.Played > SelectedDate.Date && h.Played < SelectedDate.Date.AddDays(1))
                .CountAsync() / (double)HistoryPageSize);

            StreamHistory currentlyPlaying = await dbContext.StreamHistory
                .AsNoTracking()
                .OrderByDescending(h => h.Played)
                .FirstOrDefaultAsync(h => h.Played.AddSeconds(h.TrackStreamInfo.Track.Length) > now);


            HistoryItems = await dbContext.StreamHistory
                .AsNoTracking()
                .Where(h => h.Played > SelectedDate.Date && h.Played < SelectedDate.Date.AddDays(1))
                .OrderByDescending(h => h.Played)
                .Skip(HistoryPageSize * (HistoryCurrentPage - 1))
                .Take(HistoryPageSize)
                .Select(h => new HistoryViewModel
                {
                    AlbumTitle = h.TrackStreamInfo.Track.Disc.Album.Title,
                    TrackTitle = h.TrackStreamInfo.Track.Title,
                    Length = h.TrackStreamInfo.Track.Duration,
                    AlbumId = h.TrackStreamInfo.Track.Disc.Album.Id,
                    HasAlbumCover = h.TrackStreamInfo.Track.Disc.Album.AlbumCover != null,
                    Played = h.Played,
                    CurrentlyPlaying = h.Id == currentlyPlaying.Id,
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
                .ToListAsync();
        }
    }
}
