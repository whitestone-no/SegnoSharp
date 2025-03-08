using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.AlbumEditor.Components.Pages
{
    public partial class EditAlbum : IDisposable
    {
        [Parameter] public int Id { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private ISystemClock SystemClock { get; set; }

        private SegnoSharpDbContext DbContext { get; set; }
        private Album Album { get; set; }
        private List<PersonGroup> PersonGroups { get; set; }
        private int SelectedPersonGroupId { get; set; }
        private bool AlbumCoverFileSizeError { get; set; }
        private List<MediaType> MediaTypes { get; set; }

        private EditContext _editContext;

        private Track _currentlyDraggingTrack;
        private TrackGroup _currentlyDraggingTrackGroup;
        private Track _currentlyDraggingOverTrack;

        protected override async Task OnInitializedAsync()
        {
            DbContext = await DbFactory.CreateDbContextAsync();

            if (Id == 0)
            {
                Album = new Album
                {
                    AlbumPersonGroupPersonRelations = [],
                    Discs = [],
                    Genres = [],
                    RecordLabels = [],
                    Added = SystemClock.Now
                };
                DbContext.Albums.Add(Album);
            }
            else
            {
                Album = await DbContext.Albums
                    .Include(a => a.Genres)
                    .Include(a => a.RecordLabels)
                    .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.Persons)
                    .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.PersonGroup)
                    .Include(a => a.AlbumCover).ThenInclude(c => c.AlbumCoverData)
                    .Include(a => a.Discs).ThenInclude(d => d.Tracks)
                    .Include(a => a.Discs).ThenInclude(d => d.MediaTypes)
                    .Include(a => a.Discs).ThenInclude(d => d.TrackGroups)
                    .FirstOrDefaultAsync(a => a.Id == Id);
            }

            _editContext = new EditContext(Album);

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
            string[] searchTerms = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            IQueryable<Person> persons = DbContext.Persons.AsQueryable();

            foreach (string term in searchTerms)
            {
                persons = persons.Where(p => EF.Functions.Like(p.FirstName, "%" + term + "%") || EF.Functions.Like(p.LastName, "%" + term + "%"));
            }

            return await persons.ToListAsync();
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
                if (!_editContext.Validate())
                {
                    return;
                }

                await Save();
            }

            NavigationManager.NavigateTo("/admin/albums");
        }

        private async Task Save()
        {
            if (!_editContext.Validate())
            {
                return;
            }

            // Get all persons from credit groups
            List<Person> allPersons = Album.AlbumPersonGroupPersonRelations
                .SelectMany(apg => apg.Persons)
                .Distinct()
                .ToList();

            foreach (AlbumPersonGroupPersonRelation personRelation in Album.AlbumPersonGroupPersonRelations)
            {
                List<Person> persons = [];

                foreach (Person person in personRelation.Persons)
                {
                    if (person.Id != 0)
                    {
                        // The person already exists in the database if it has an ID, so just add it without further ado.
                        persons.Add(person);
                        continue;
                    }

                    // To prevent multiple copies of the same person between different credit groups
                    // get the new person from the distinct list of persons, but ignore the variant number as
                    // that may have been set by another credit group.
                    Person newPerson = allPersons
                        .FirstOrDefault(p =>
                            p.Id == person.Id &&
                            p.FirstName == person.FirstName &&
                            p.LastName == person.LastName);

                    if (newPerson == null)
                    {
                        // This shouldn't really happen, but just continue to the next person if it does.
                        continue;
                    }

                    // Find the correct variant number, but only if it hasn't already been set
                    if (newPerson.Version == 0)
                    {
                        try
                        {
                            ushort variant = await DbContext.Persons
                                .Where(p => p.FirstName == newPerson.FirstName && p.LastName == newPerson.LastName)
                                .MaxAsync(p => p.Version);

                            newPerson.Version = (ushort)(variant + 1);
                        }
                        catch (InvalidOperationException)
                        {
                            // Ignored as you can't get the max from a result set containing nothing (brand new person, not a variant)
                        }
                    }

                    // Add the new person to the database
                    persons.Add(newPerson);
                }

                personRelation.Persons = persons;
            }

            await DbContext.SaveChangesAsync();
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

        private void DragStart(TrackGroup trackGroup)
        {
            _currentlyDraggingTrackGroup = trackGroup;
        }

        private void HandleDrop(Track targetTrack)
        {
            if (_currentlyDraggingTrack != null)
            {
                UpdateTrackNumberByDrag(targetTrack);
            }

            if (_currentlyDraggingTrackGroup != null)
            {
                UpdateTrackGroupByDrag(targetTrack);
            }
        }

        private void UpdateTrackGroupByDrag(Track targetTrack)
        {
            _currentlyDraggingTrackGroup.GroupBeforeTrackNumber = targetTrack.TrackNumber;

            if (_currentlyDraggingTrackGroup.Disc.Id == targetTrack.DiscId)
            {
                return;
            }

            // If dragging group to another disc
            _currentlyDraggingTrackGroup.Disc.TrackGroups.Remove(_currentlyDraggingTrackGroup);
            _currentlyDraggingTrackGroup.Disc = targetTrack.Disc;
            targetTrack.Disc.TrackGroups.Add(_currentlyDraggingTrackGroup);
        }

        private void UpdateTrackNumberByDrag(Track targetTrack)
        {
            ushort newTrackNumber = targetTrack.TrackNumber;
            ushort oldTrackNumber = _currentlyDraggingTrack.TrackNumber;

            // If dragging track to another disc
            if (_currentlyDraggingTrack.DiscId != targetTrack.DiscId)
            {
                // Clean up the track numbers on the previous disc
                foreach (Track previousDiscTrack in _currentlyDraggingTrack.Disc.Tracks.Where(t => t.TrackNumber > oldTrackNumber))
                {
                    previousDiscTrack.TrackNumber = (ushort)(previousDiscTrack.TrackNumber - 1);
                }

                // Update track numbers greater than the new track number
                foreach (Track newDiscTrack in targetTrack.Disc.Tracks.Where(t => t.TrackNumber >= newTrackNumber))
                {
                    newDiscTrack.TrackNumber = (ushort)(newDiscTrack.TrackNumber + 1);
                }

                _currentlyDraggingTrack.Disc.Tracks.Remove(_currentlyDraggingTrack);
                _currentlyDraggingTrack.DiscId = targetTrack.DiscId;
                _currentlyDraggingTrack.Disc = targetTrack.Disc;
                targetTrack.Disc.Tracks.Add(_currentlyDraggingTrack);
            }
            // If dragging within same disc
            else
            {
                // If moving "down"
                if (newTrackNumber > oldTrackNumber)
                {
                    newTrackNumber = (ushort)(newTrackNumber - 1);

                    foreach (Track moveTrack in targetTrack.Disc.Tracks.Where(t => t.TrackNumber <= newTrackNumber && t.TrackNumber > oldTrackNumber && t.Id != _currentlyDraggingTrack.Id))
                    {
                        moveTrack.TrackNumber = (ushort)(moveTrack.TrackNumber - 1);
                    }

                }
                // If moving "up"
                else if (newTrackNumber < oldTrackNumber)
                {
                    foreach (Track moveTrack in targetTrack.Disc.Tracks.Where(t => t.TrackNumber < oldTrackNumber && t.TrackNumber >= newTrackNumber && t.Id != _currentlyDraggingTrack.Id))
                    {
                        moveTrack.TrackNumber = (ushort)(moveTrack.TrackNumber + 1);
                    }
                }
            }

            _currentlyDraggingTrack.TrackNumber = newTrackNumber;
        }

        private void HandleDragEnd()
        {
            _currentlyDraggingTrack = null;
            _currentlyDraggingTrackGroup = null;
            _currentlyDraggingOverTrack = null;
        }

        private void HandleDragEnter(Track track)
        {
            if (_currentlyDraggingTrack != null || _currentlyDraggingTrackGroup != null)
            {
                _currentlyDraggingOverTrack = track;
            }
        }

        private async Task OnBeforeInternalNavigation(LocationChangingContext context)
        {
            if (DbContext.ChangeTracker.HasChanges())
            {
                var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "You have unsaved changes. Are you sure you want to close?");
                if (!confirmed)
                {
                    context.PreventNavigation();
                }
            }
        }

        private async Task DeleteAlbum()
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this album and all its media and tracks? This operation cannot be undone!");
            if (!confirmed)
            {
                return;
            }

            // Get album with all relations so that cascade deleting will delete without foreign key errors
            Album album = await DbContext.Albums
                .Include(a => a.Genres)
                .Include(a => a.RecordLabels)
                .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.Persons)
                .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.PersonGroup)
                .Include(a => a.AlbumCover).ThenInclude(c => c.AlbumCoverData)
                .Include(a => a.Discs).ThenInclude(d => d.Tracks).ThenInclude(t => t.TrackStreamInfo).ThenInclude(tsi => tsi.StreamHistory)
                .Include(a => a.Discs).ThenInclude(d => d.Tracks).ThenInclude(t => t.TrackStreamInfo).ThenInclude(tsi => tsi.StreamQueue)
                .Include(a => a.Discs).ThenInclude(d => d.Tracks).ThenInclude(t => t.TrackPersonGroupPersonRelations).ThenInclude(r => r.Persons)
                .Include(a => a.Discs).ThenInclude(d => d.Tracks).ThenInclude(t => t.TrackPersonGroupPersonRelations).ThenInclude(r => r.PersonGroup)
                .Include(a => a.Discs).ThenInclude(d => d.MediaTypes)
                .Include(a => a.Discs).ThenInclude(d => d.TrackGroups)
                .FirstOrDefaultAsync(a => a.Id == Id);

            DbContext.Albums.Remove(album);

            await DbContext.SaveChangesAsync();

            NavigationManager.NavigateTo($"/admin/albums");
        }

        private void RemoveTrackGroup(TrackGroup trackGroup)
        {
            trackGroup.Disc.TrackGroups.Remove(trackGroup);
        }

        private void AddTrackGroup(Disc disc)
        {
            disc.TrackGroups.Add(new TrackGroup { Disc = disc, DiscId = disc.Id, GroupBeforeTrackNumber = 1, Name = "New Track Group" });
        }

        private async Task DeleteDisc(Disc disc)
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this disc?");
            if (!confirmed)
            {
                return;
            }

            disc.Album.Discs.Remove(disc);
        }

        private void AddDisc()
        {
            Album.Discs.Add(new Disc
            {
                MediaTypes = [],
                TrackGroups = [],
                Tracks = [],
                DiscNumber = (byte)(Album.Discs.Count + 1)
            });
        }

        private static void AddTrack(Disc disc)
        {
            disc.Tracks.Add(new Track
            {
                Title = "<Untitled>",
                TrackPersonGroupPersonRelations = [],
                TrackNumber = (ushort)(disc.Tracks.Count + 1)
            });
        }
    }
}
