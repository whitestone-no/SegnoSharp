using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.Playlist.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Pages
{
    public partial class Playlist : IDisposable
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        private List<PlaylistViewModel> PlaylistItems { get; set; } = [];

        private SegnoSharpDbContext _dbContext;

        protected override async Task OnInitializedAsync()
        {
            _dbContext = await DbFactory.CreateDbContextAsync();

            List<StreamQueue> playlist = await _dbContext.StreamQueue
                .Include(t => t.TrackStreamInfo)
                .ThenInclude(t => t.Track)
                .ThenInclude(t => t.TrackPersonGroupPersonRelations)
                .ThenInclude(r => r.Persons)
                .Include(t => t.TrackStreamInfo)
                .ThenInclude(t => t.Track)
                .ThenInclude(t => t.TrackPersonGroupPersonRelations)
                .ThenInclude(r => r.PersonGroup)
                .ThenInclude(g => g.PersonGroupStreamInfo)
                .Include(t => t.TrackStreamInfo)
                .ThenInclude(t => t.Track)
                .ThenInclude(t => t.Disc)
                .ThenInclude(d => d.Album)
                .ThenInclude(a => a.AlbumPersonGroupPersonRelations)
                .ThenInclude(r => r.Persons)
                .Include(t => t.TrackStreamInfo)
                .ThenInclude(t => t.Track)
                .ThenInclude(t => t.Disc)
                .ThenInclude(d => d.Album)
                .ThenInclude(a => a.AlbumPersonGroupPersonRelations)
                .ThenInclude(r => r.PersonGroup)
                .ThenInclude(g => g.PersonGroupStreamInfo)
                .OrderBy(t => t.SortOrder)
                .AsSplitQuery()
                .ToListAsync();

            foreach (StreamQueue playlistItem in playlist)
            {

                var artists = string.Empty;

                string[] trackPeople = playlistItem.TrackStreamInfo.Track.TrackPersonGroupPersonRelations
                    .Where(g => g.PersonGroup.PersonGroupStreamInfo is { IncludeInAutoPlaylist: true })
                    .SelectMany(g => g.Persons)
                    .Distinct()
                    .Select(p => (p.FirstName + " " + p.LastName).Trim())
                    .ToArray();

                if (trackPeople.Length > 0)
                {
                    artists = string.Join(", ", trackPeople);
                }
                else
                {
                    string[] albumPeople = playlistItem.TrackStreamInfo.Track.Disc.Album.AlbumPersonGroupPersonRelations
                        .Where(g => g.PersonGroup.PersonGroupStreamInfo is { IncludeInAutoPlaylist: true })
                        .SelectMany(g => g.Persons)
                        .Distinct()
                        .Select(p => (p.FirstName + " " + p.LastName).Trim())
                        .ToArray();

                    if (albumPeople.Length > 0)
                    {
                        artists = string.Join(", ", albumPeople);
                    }
                }

                PlaylistViewModel vm = new()
                {
                    Album = playlistItem.TrackStreamInfo.Track.Disc.Album.Title,
                    Track = playlistItem.TrackStreamInfo.Track.Title,
                    Artist = artists,
                    Duration = playlistItem.TrackStreamInfo.Track.Duration
                };

                PlaylistItems.Add(vm);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext?.Dispose();
            }
        }
    }
}
