using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Common.Events;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Modules.Playlist.ViewModels;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Pages
{
    public partial class Playlist : IEventHandler<PlaylistUpdated>
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private IHashingUtil HashingUtil { get; set; }
        [Inject] private ISystemClock SystemClock { get; set; }
        [Inject] private ILogger<Playlist> Logger { get; set; }

        private List<PlaylistViewModel> PlaylistItems { get; set; } = [];
        private PlaylistViewModel CurrentlyPlaying { get; set; }

        protected override void OnInitialized()
        {
            Cambion.Register(this);

            HandleEvent(new PlaylistUpdated());
        }

        // ReSharper disable once AsyncVoidMethod
        public async void HandleEvent(PlaylistUpdated input)
        {

            try
            {
                SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

                PlaylistItems = await dbContext.StreamQueue
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
                        SortOrder = q.SortOrder,
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
                    .ToListAsync();

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
