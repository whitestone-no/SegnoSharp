using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Pages.Admin.AlbumEditor
{
    public partial class EditAlbum : IDisposable
    {
        [Parameter] public int Id { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        private SegnoSharpDbContext DbContext { get; set; }
        private Album Album { get; set; }
        private List<PersonGroup> PersonGroups { get; set; }
        private int SelectedPersonGroupId { get; set; }
        private bool AlbumCoverFileSizeError { get; set; }
        private List<MediaType> MediaTypes { get; set; }

        private Track _currentlyDraggingTrack = null;
        private Track _currentlyDraggingOverTrack = null;

        protected override async Task OnInitializedAsync()
        {
            DbContext = await DbFactory.CreateDbContextAsync();

            Album = await DbContext.Albums
                .Include(a => a.Genres)
                .Include(a => a.RecordLabels)
                .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.Persons)
                .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.PersonGroup)
                .Include(a => a.AlbumCover).ThenInclude(c => c.AlbumCoverData)
                .Include(a => a.Discs).ThenInclude(d => d.Tracks)
                .FirstOrDefaultAsync(a => a.Id == Id);

            PersonGroups = await DbContext.PersonGroups
                .Where(g => g.Type == PersonGroupType.Album)
                .ToListAsync();

            MediaTypes = await DbContext.MediaTypes.ToListAsync();
        }

        private async Task<IEnumerable<Genre>> ExecuteGenreSearch(string searchTerm)
        {
            return await DbContext.Genres
                .Where(g => EF.Functions.Like(g.Name, "%" + searchTerm + "%"))
                .ToListAsync();
        }

        private async Task<IEnumerable<Person>> ExecutePersonSearch(string searchTerm)
        {
            var p = new Person { TagName = searchTerm };

            return await DbContext.Persons
                    .Where(pp => EF.Functions.Like(pp.LastName, "%" + p.LastName + "%") || EF.Functions.Like(pp.FirstName, "%" + p.FirstName + "%"))
                    .ToListAsync();
        }

        private async Task<IEnumerable<RecordLabel>> ExecuteRecordLabelSearch(string searchTerm)
        {
            return await DbContext.RecordLabels
                .Where(rl => EF.Functions.Like(rl.Name, "%" + searchTerm + "%"))
                .ToListAsync();
        }

        private void AddPersonGroup()
        {
            Album.AlbumPersonGroupPersonRelations.Add(new AlbumPersonGroupPersonRelation
            {
                PersonGroup = PersonGroups.First(pg => pg.Id == SelectedPersonGroupId)
            });
        }
        private async Task AddAlbumCover(InputFileChangeEventArgs e)
        {
            if (e.FileCount <= 0)
            {
                return;
            }

            using MemoryStream ms = new();
            try
            {
                await e.File.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms); // Max image size = 5MB
            }
            catch
            {
                AlbumCoverFileSizeError = true;
                return;
            }

            AlbumCoverFileSizeError = false;

            byte[] fileBytes = ms.ToArray();

            Album.AlbumCover = new AlbumCover
            {
                Filename = e.File.Name,
                Filesize = Convert.ToUInt32(e.File.Size),
                Mime = e.File.ContentType,
                AlbumCoverData = new AlbumCoverData
                {
                    Data = fileBytes
                }
            };
        }

        private void RemoveAlbumCover()
        {
            Album.AlbumCover = null;
        }

        private void RemovePersonGroup(AlbumPersonGroupPersonRelation personGroupRelation)
        {
            Album.AlbumPersonGroupPersonRelations.Remove(personGroupRelation);
        }

        private async Task Close(bool save = false)
        {
            if (save)
            {
                await DbContext.SaveChangesAsync();
            }

            NavigationManager.NavigateTo("/admin/albums");
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
                DbContext?.Dispose();
            }
        }

        private void DragStart(Track track)
        {
            _currentlyDraggingTrack = track;
        }

        private void HandleDrop(Track targetTrack)
        {
            _currentlyDraggingTrack.Disc.Tracks.Remove(_currentlyDraggingTrack);

            foreach (Track trackAbove in _currentlyDraggingTrack.Disc.Tracks.Where(t => t.TrackNumber > _currentlyDraggingTrack.TrackNumber))
            {
                trackAbove.TrackNumber = (ushort)(trackAbove.TrackNumber - 1);
            }

            foreach (Track destinationTrackBelow in targetTrack.Disc.Tracks.Where(t => t.TrackNumber > targetTrack.TrackNumber))
            {
                destinationTrackBelow.TrackNumber = (ushort)(destinationTrackBelow.TrackNumber + 1);
            }

            _currentlyDraggingTrack.TrackNumber = (ushort)(targetTrack.TrackNumber + 1);
            _currentlyDraggingTrack.Disc = targetTrack.Disc;
            targetTrack.Disc.Tracks.Add(_currentlyDraggingTrack);
        }

        private void HandleDragEnd()
        {
            _currentlyDraggingTrack = null;
            _currentlyDraggingOverTrack = null;
        }

        private void HandleDragEnter(Track track)
        {
            _currentlyDraggingOverTrack = track;
        }
    }
}
