using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.Playlist.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Pages
{
    public partial class History : IDisposable
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        private List<HistoryViewModel> HistoryItems { get; set; } = [];

        private SegnoSharpDbContext _dbContext;

        protected override async Task OnInitializedAsync()
        {
            _dbContext = await DbFactory.CreateDbContextAsync();

            List<StreamHistory> history = await _dbContext.StreamHistory
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
                .OrderByDescending(t => t.Played)
                .Take(100)
                .AsSplitQuery()
                .ToListAsync();

            foreach (StreamHistory historyItem in history)
            {

                var artists = string.Empty;

                string[] trackPeople = historyItem.TrackStreamInfo.Track.TrackPersonGroupPersonRelations
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
                    string[] albumPeople = historyItem.TrackStreamInfo.Track.Disc.Album.AlbumPersonGroupPersonRelations
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

                HistoryViewModel vm = new()
                {
                    Album = historyItem.TrackStreamInfo.Track.Disc.Album.Title,
                    Track = historyItem.TrackStreamInfo.Track.Title,
                    Artist = artists,
                    Played = historyItem.Played
                };

                HistoryItems.Add(vm);
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
