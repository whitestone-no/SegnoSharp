using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Modules.Playlist.ViewModels;
using System.Linq;
using System;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Common.Events;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Pages.Admin
{
    public partial class Playlist : IEventHandler<PlaylistUpdated>
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private ICambion Cambion { get; set; }
        [Inject] private PlaylistQueueLocker QueueLocker { get; set; }

        private List<PlaylistViewModel> PlaylistModel { get; set; } = [];

        public SearchViewModel SearchModel { get; set; } = new();
        public List<SearchResultViewModel> SearchResults { get; set; } = [];

        private const int SearchPageSize = 20;
        private int SearchTotalPages { get; set; }
        private int SearchCurrentPage { get; set; } = 1;

        private PlaylistViewModel _currentlyDraggingPlaylistItem;
        private SearchResultViewModel _currentlyDraggingSearchItem;
        private PlaylistViewModel _currentlyDraggingOverPlaylistItem;
        private bool _currentlyDraggingOverPlaylistEnd;

        protected override void OnInitialized()
        {
            Cambion.Register(this);

            HandleEvent(new PlaylistUpdated());
        }

        private async Task DoSearchAsync()
        {
            if (string.IsNullOrEmpty(SearchModel.SearchTerm))
            {
                SearchResults = [];
                SearchTotalPages = 1;
                SearchCurrentPage = 1;
            }

            SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            SearchTotalPages = (int)Math.Ceiling(await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(tsi =>
                    // Public/AutoPlaylist filters
                    (!SearchModel.OnlyPublic || (SearchModel.OnlyPublic && tsi.Track.Disc.Album.IsPublic)) &&
                    (!SearchModel.OnlyAutoPlaylist || (SearchModel.OnlyAutoPlaylist && tsi.IncludeInAutoPlaylist)) &&

                    // Search criteria
                    (
                        (SearchModel.SearchForFilename &&
                            EF.Functions.Like(tsi.FilePath, "%" + SearchModel.SearchTerm + "%")) ||

                        (SearchModel.SearchForAlbum &&
                            EF.Functions.Like(tsi.Track.Disc.Album.Title, "%" + SearchModel.SearchTerm + "%")) ||

                        (SearchModel.SearchForTrack &&
                            EF.Functions.Like(tsi.Track.Title, "%" + SearchModel.SearchTerm + "%")) ||

                        (SearchModel.SearchForArtist &&
                            (
                                // Search in track artists
                                (tsi.Track.TrackPersonGroupPersonRelations.Any(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                                    r.Persons.Any(p =>
                                        EF.Functions.Like(p.FirstName, "%" + SearchModel.SearchTerm + "%") ||
                                        EF.Functions.Like(p.LastName, "%" + SearchModel.SearchTerm + "%")
                                    )
                                )) ||
                                // Search in album artists
                                (tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                                    r.Persons.Any(p =>
                                        EF.Functions.Like(p.FirstName, "%" + SearchModel.SearchTerm + "%") ||
                                        EF.Functions.Like(p.LastName, "%" + SearchModel.SearchTerm + "%")
                                    )
                                ))
                            )
                        )
                    )
                )
                .CountAsync() / (double)SearchPageSize);

            await OnSearchPageChanged(1);
        }

        private async Task OnSearchPageChanged(int page)
        {
            SearchCurrentPage = page;

            SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            SearchResults = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(tsi =>
                    // Public/AutoPlaylist filters
                    (!SearchModel.OnlyPublic || (SearchModel.OnlyPublic && tsi.Track.Disc.Album.IsPublic)) &&
                    (!SearchModel.OnlyAutoPlaylist || (SearchModel.OnlyAutoPlaylist && tsi.IncludeInAutoPlaylist)) &&

                    // Search criteria
                    (
                        (SearchModel.SearchForFilename &&
                            EF.Functions.Like(tsi.FilePath, "%" + SearchModel.SearchTerm + "%")) ||

                        (SearchModel.SearchForAlbum &&
                            EF.Functions.Like(tsi.Track.Disc.Album.Title, "%" + SearchModel.SearchTerm + "%")) ||

                        (SearchModel.SearchForTrack &&
                            EF.Functions.Like(tsi.Track.Title, "%" + SearchModel.SearchTerm + "%")) ||

                        (SearchModel.SearchForArtist &&
                            (
                                // Search in track artists
                                (tsi.Track.TrackPersonGroupPersonRelations.Any(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                                    r.Persons.Any(p =>
                                        EF.Functions.Like(p.FirstName, "%" + SearchModel.SearchTerm + "%") ||
                                        EF.Functions.Like(p.LastName, "%" + SearchModel.SearchTerm + "%")
                                    )
                                )) ||
                                // Search in album artists
                                (tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                                    r.Persons.Any(p =>
                                        EF.Functions.Like(p.FirstName, "%" + SearchModel.SearchTerm + "%") ||
                                        EF.Functions.Like(p.LastName, "%" + SearchModel.SearchTerm + "%")
                                    )
                                ))
                            )
                        )
                    )
                )
                .Skip(SearchPageSize * (SearchCurrentPage - 1))
                .Take(SearchPageSize)
                .Select(tsi => new SearchResultViewModel
                {
                    TrackStreamInfoId = tsi.Id,
                    AlbumTitle = tsi.Track.Disc.Album.Title,
                    TrackTitle = tsi.Track.Title,
                    Length = tsi.Track.Duration,
                    TrackArtists = string.Join(", ",
                        tsi.Track.TrackPersonGroupPersonRelations
                            .Where(r =>
                                r.PersonGroup.PersonGroupStreamInfo != null &&
                                r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                            .SelectMany(r =>
                                r.Persons.Select(p =>
                                    p.FirstName == null ? p.LastName : p.FirstName + " " + p.LastName))),
                    AlbumArtists = string.Join(", ",
                        tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations
                            .Where(r =>
                                r.PersonGroup.PersonGroupStreamInfo != null &&
                                r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                            .SelectMany(r =>
                                r.Persons.Select(p =>
                                    p.FirstName == null ? p.LastName : p.FirstName + " " + p.LastName)))
                })
                .ToListAsync();
        }

        // ReSharper disable once AsyncVoidMethod
        public async void HandleEvent(PlaylistUpdated input)
        {
            try
            {
                await QueueLocker.LockQueueAsync();

                SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

                PlaylistModel = await dbContext.StreamQueue
                    .AsNoTracking()
                    .OrderBy(q => q.SortOrder)
                    .Select(q => new PlaylistViewModel
                    {
                        AlbumTitle = q.TrackStreamInfo.Track.Disc.Album.Title,
                        TrackTitle = q.TrackStreamInfo.Track.Title,
                        Length = q.TrackStreamInfo.Track.Duration,
                        QueueId = q.Id,
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

                await InvokeAsync(StateHasChanged);
            }
            finally
            {
                QueueLocker.UnlockQueue();
            }

        }

        private void HandleDragStart(PlaylistViewModel item)
        {
            _currentlyDraggingPlaylistItem = item;
        }

        private void HandleDragStart(SearchResultViewModel item)
        {
            _currentlyDraggingSearchItem = item;
        }

        private async Task HandleDrop(PlaylistViewModel targetPlaylistItem)
        {
            try
            {
                await QueueLocker.LockQueueAsync();

                SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

                if (_currentlyDraggingSearchItem != null)
                {
                    var newSortOrder = (ushort)(targetPlaylistItem?.SortOrder ?? PlaylistModel.Max(p => p.SortOrder) + 1);

                    foreach (StreamQueue moveQueue in await dbContext.StreamQueue.Where(q => q.SortOrder >= newSortOrder).ToListAsync())
                    {
                        moveQueue.SortOrder = (ushort)(moveQueue.SortOrder + 1);
                    }

                    dbContext.StreamQueue.Add(new StreamQueue
                    {
                        SortOrder = newSortOrder,
                        TrackStreamInfo = await dbContext.TrackStreamInfos
                            .FirstOrDefaultAsync(tsi => tsi.Id == _currentlyDraggingSearchItem.TrackStreamInfoId)
                    });

                    await dbContext.SaveChangesAsync();

                    await Cambion.PublishEventAsync(new PlaylistUpdated());
                }
                else if (_currentlyDraggingPlaylistItem != null)
                {


                    var newSortOrder = (ushort)(targetPlaylistItem?.SortOrder ?? PlaylistModel.Max(p => p.SortOrder) + 1);
                    ushort oldSortOrder = _currentlyDraggingPlaylistItem.SortOrder;

                    // If moving "down"
                    if (newSortOrder > oldSortOrder)
                    {
                        newSortOrder = (ushort)(newSortOrder - 1);
                        foreach (StreamQueue moveQueue in await dbContext.StreamQueue.Where(q => q.SortOrder <= newSortOrder && q.SortOrder > oldSortOrder && q.Id != _currentlyDraggingPlaylistItem.QueueId).ToListAsync())
                        {
                            moveQueue.SortOrder = (ushort)(moveQueue.SortOrder - 1);
                        }
                    }
                    // If moving "up"
                    else if (newSortOrder < oldSortOrder)
                    {
                        foreach (StreamQueue moveGroup in await dbContext.StreamQueue.Where(g => g.SortOrder < oldSortOrder && g.SortOrder >= newSortOrder && g.Id != _currentlyDraggingPlaylistItem.QueueId).ToListAsync())
                        {
                            moveGroup.SortOrder = (ushort)(moveGroup.SortOrder + 1);
                        }
                    }

                    StreamQueue currentlyDragging = await dbContext.StreamQueue.FirstAsync(q => q.Id == _currentlyDraggingPlaylistItem.QueueId);
                    currentlyDragging.SortOrder = newSortOrder;

                    await dbContext.SaveChangesAsync();

                    await Cambion.PublishEventAsync(new PlaylistUpdated());
                }
            }
            finally
            {
                QueueLocker.UnlockQueue();
            }
        }

        private void HandleDragEnd()
        {
            _currentlyDraggingPlaylistItem = null;
            _currentlyDraggingSearchItem = null;
            _currentlyDraggingOverPlaylistItem = null;
            _currentlyDraggingOverPlaylistEnd = false;
        }

        private void HandleDragEnter(PlaylistViewModel item)
        {
            _currentlyDraggingOverPlaylistItem = item;
            _currentlyDraggingOverPlaylistEnd = item == null;
        }

        private async Task AddTrackToQueueTop(int trackStreamInfoId)
        {
            try
            {
                await QueueLocker.LockQueueAsync();

                SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

                List<StreamQueue> queue = await dbContext.StreamQueue.ToListAsync();

                foreach (StreamQueue queueItem in queue)
                {
                    queueItem.SortOrder++;
                }

                dbContext.StreamQueue.Add(new StreamQueue
                {
                    SortOrder = 1,
                    TrackStreamInfo = await dbContext.TrackStreamInfos.FirstAsync(tsi => tsi.Id == trackStreamInfoId)
                });

                await dbContext.SaveChangesAsync();

                await Cambion.PublishEventAsync(new PlaylistUpdated());
            }
            finally
            {
                QueueLocker.UnlockQueue();
            }
        }

        private async Task AddTrackToQueueBottom(int trackStreamInfoId)
        {
            try
            {
                await QueueLocker.LockQueueAsync();

                SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

                ushort maxSortOrder = await dbContext.StreamQueue.MaxAsync(q => q.SortOrder);

                dbContext.StreamQueue.Add(new StreamQueue
                {
                    SortOrder = (ushort)(maxSortOrder + 1),
                    TrackStreamInfo = await dbContext.TrackStreamInfos.FirstAsync(tsi => tsi.Id == trackStreamInfoId)
                });

                await dbContext.SaveChangesAsync();

                await Cambion.PublishEventAsync(new PlaylistUpdated());
            }
            finally
            {
                QueueLocker.UnlockQueue();
            }
        }

        private async Task RemoveFromPlaylist(PlaylistViewModel playlistItem)
        {
            try
            {
                await QueueLocker.LockQueueAsync();

                SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

                StreamQueue queueItem = await dbContext.StreamQueue.FirstOrDefaultAsync(q => q.Id == playlistItem.QueueId);

                dbContext.StreamQueue.Remove(queueItem);

                await dbContext.StreamQueue.Where(q => q.SortOrder > queueItem.SortOrder)
                    .ForEachAsync(q => q.SortOrder--);

                await dbContext.SaveChangesAsync();

                await Cambion.PublishEventAsync(new PlaylistUpdated());
            }
            finally
            {
                QueueLocker.UnlockQueue();
            }
        }
    }
}
