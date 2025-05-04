using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.AlbumEditor.Components.Pages
{
    public partial class EditTrack : IDisposable
    {
        [Parameter] public int Id { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }

        private SegnoSharpDbContext DbContext { get; set; }
        private Track Track { get; set; }
        private List<PersonGroup> PersonGroups { get; set; }
        private int SelectedPersonGroupId { get; set; }
        private bool Invalid => string.IsNullOrEmpty(Track.Title) || (Track.TrackStreamInfo != null && string.IsNullOrEmpty(Track.TrackStreamInfo.FilePath));

        private EditContext _editContext;

        private int _originalTrackNumber;

        protected override async Task OnInitializedAsync()
        {
            DbContext = await DbFactory.CreateDbContextAsync();

            Track = await DbContext.Tracks
                .Include(t => t.Disc).ThenInclude(d => d.Album)
                .Include(t => t.TrackStreamInfo)
                .Include(t => t.TrackPersonGroupPersonRelations).ThenInclude(r => r.Persons)
                .Include(t => t.TrackPersonGroupPersonRelations).ThenInclude(r => r.PersonGroup)
                .FirstOrDefaultAsync(t => t.Id == Id);

            PersonGroups = await DbContext.PersonGroups
                .Where(g => g.Type == PersonGroupType.Track)
                .ToListAsync();

            _originalTrackNumber = Track.TrackNumber;

            _editContext = new EditContext(Track);
        }

        private async Task Close(bool save = false)
        {
            if (save)
            {
                if (!_editContext.Validate())
                {
                    return;
                }

                // This code is duplicated between EditTrack and EditAlbum. Should be combined, somehow
                List<Person> allPersons = Track.TrackPersonGroupPersonRelations
                    .SelectMany(apg => apg.Persons)
                    .Distinct()
                    .ToList();

                foreach (TrackPersonGroupPersonRelation personRelation in Track.TrackPersonGroupPersonRelations)
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

                if (Track.TrackNumber != _originalTrackNumber)
                {
                    Disc disc = await DbContext.Discs
                        .Include(d => d.Tracks)
                        .FirstOrDefaultAsync(d => d.Id == Track.Disc.Id);

                    if (Track.TrackNumber <= 0)
                    {
                        Track.TrackNumber = 1;
                    }
                    else if (Track.TrackNumber > disc.Tracks.Count)
                    {
                        Track.TrackNumber = (ushort)disc.Tracks.Count;
                    }

                    if (Track.TrackNumber != _originalTrackNumber)
                    {
                        if (Track.TrackNumber > _originalTrackNumber)
                        {
                            foreach (Track moveTrack in disc.Tracks.Where(t => t.TrackNumber <= Track.TrackNumber && t.TrackNumber > _originalTrackNumber && t.Id != Track.Id))
                            {
                                moveTrack.TrackNumber = (ushort)(moveTrack.TrackNumber - 1);
                            }

                        }
                        else if (Track.TrackNumber < _originalTrackNumber)
                        {
                            foreach (Track moveTrack in disc.Tracks.Where(t => t.TrackNumber < _originalTrackNumber && t.TrackNumber >= Track.TrackNumber && t.Id != Track.Id))
                            {
                                moveTrack.TrackNumber = (ushort)(moveTrack.TrackNumber + 1);
                            }
                        }

                        await DbContext.SaveChangesAsync();
                    }
                }
            }

            NavigationManager.NavigateTo($"admin/albums/{Track.Disc.Album.Id}");
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

        private void AddPersonGroup()
        {
            Track.TrackPersonGroupPersonRelations.Add(new TrackPersonGroupPersonRelation
            {
                PersonGroup = PersonGroups.First(pg => pg.Id == SelectedPersonGroupId)
            });
        }

        private void RemovePersonGroup(TrackPersonGroupPersonRelation personGroupRelation)
        {
            Track.TrackPersonGroupPersonRelations.Remove(personGroupRelation);
        }

        private void AddStreamInfo()
        {
            Track.TrackStreamInfo = new TrackStreamInfo();
        }

        private async Task RemoveStreamInfo()
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete the stream info? This will remove the track from all playlist history!");
            if (!confirmed)
            {
                return;
            }

            Track.TrackStreamInfo = null;
        }

        private async Task DeleteTrack()
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this track? This operation cannot be undone!");
            if (!confirmed)
            {
                return;
            }

            DbContext.Tracks.Remove(Track);
            await DbContext.SaveChangesAsync();

            NavigationManager.NavigateTo($"admin/albums/{Track.Disc.Album.Id}");
        }
    }
}
