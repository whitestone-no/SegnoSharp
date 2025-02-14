using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models;
using Whitestone.SegnoSharp.Modules.MediaImporter.ViewModels;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.Components.Pages
{
    public partial class Step4
    {
        [Inject] private ImportState ImporterState { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        private int NewAlbumCount { get; set; }
        private int ExistingAlbumCount { get; set; }
        private int NewAlbumTracksCount { get; set; }
        private int ExistingAlbumTracksCount { get; set; }
        private bool ImportComplete { get; set; }
        private bool ImportInProgress { get; set; }

        protected override void OnInitialized()
        {
            if (ImporterState.AlbumsToImport == null)
            {
                return;
            }

            NewAlbumCount = ImporterState.AlbumsToImport.Count(a => !a.AlbumAlreadyExists);
            ExistingAlbumCount = ImporterState.AlbumsToImport.Count(a => a.AlbumAlreadyExists);
            NewAlbumTracksCount = ImporterState.AlbumsToImport
                .Where(a => !a.AlbumAlreadyExists)
                .SelectMany(a => a.Discs)
                .SelectMany(d => d.Tracks)
                .Count();
            ExistingAlbumTracksCount = ImporterState.AlbumsToImport
                .Where(a => a.AlbumAlreadyExists)
                .SelectMany(a => a.Discs)
                .SelectMany(d => d.Tracks)
                .Count();
        }

        private async Task DoImport()
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to import?");
            if (!confirmed)
            {
                return;
            }

            ImportInProgress = true;

            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            foreach (AlbumViewModel albumVm in ImporterState.AlbumsToImport)
            {
                // "Album exists" is already performed in previous step, but do it again to make sure nothing happened in the meantime
                Album album = await dbContext.Albums
                    .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(apgr => apgr.Persons)
                    .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(apgr => apgr.PersonGroup)
                    .Include(a => a.Discs)
                    .FirstOrDefaultAsync(a => a.Title == albumVm.Title);

                if (album == null)
                {
                    album = new Album
                    {
                        Added = DateTime.Now,
                        IsPublic = albumVm.IsPublic,
                        Published = albumVm.Published,
                        Title = albumVm.Title,
                        AlbumCover = albumVm.AlbumCover,
                        Genres = new List<Genre>(),
                        AlbumPersonGroupPersonRelations = new List<AlbumPersonGroupPersonRelation>(),
                        Discs = new List<Disc>()
                    };

                    dbContext.Albums.Add(album);

                    await dbContext.SaveChangesAsync();

                    foreach (Genre genreVm in albumVm.Genres)
                    {
                        Genre genre = await dbContext.Genres.FirstOrDefaultAsync(g => g.Name == genreVm.Name) ?? new Genre
                        {
                            Name = genreVm.Name
                        };

                        album.Genres.Add(genre);

                        await dbContext.SaveChangesAsync();
                    }

                    foreach (AlbumPersonGroupPersonRelation albumVmPersonRelations in albumVm.AlbumPersonGroupPersonRelations)
                    {
                        AlbumPersonGroupPersonRelation albumPersonRelations = album.AlbumPersonGroupPersonRelations.FirstOrDefault(apgp => apgp.PersonGroup.Id == albumVmPersonRelations.PersonGroup.Id);

                        if (albumPersonRelations == null)
                        {
                            PersonGroup personGroup = await dbContext.PersonGroups.FirstOrDefaultAsync(pg => pg.Id == albumVmPersonRelations.PersonGroup.Id);

                            albumPersonRelations = new AlbumPersonGroupPersonRelation
                            {
                                PersonGroup = personGroup,
                                Persons = new List<Person>()
                            };
                            album.AlbumPersonGroupPersonRelations.Add(albumPersonRelations);

                            await dbContext.SaveChangesAsync();
                        }

                        foreach (Person personVm in albumVmPersonRelations.Persons)
                        {
                            Person person = await dbContext.Persons
                                .Where(p => p.FirstName == personVm.FirstName && p.LastName == personVm.LastName)
                                .OrderBy(p => p.Version)
                                .FirstOrDefaultAsync() ?? new Person
                                {
                                    FirstName = personVm.FirstName,
                                    LastName = personVm.LastName,
                                    Version = 0
                                };
                            albumPersonRelations.Persons.Add(person);

                            await dbContext.SaveChangesAsync();
                        }
                    }
                }

                foreach (Disc disc1 in albumVm.Discs)
                {
                    var discVm = (DiscViewModel)disc1;

                    var disc = new Disc
                    {
                        Title = discVm.Title,
                        DiscNumber = discVm.DiscNumber,
                        MediaTypes = new List<MediaType>(),
                        Tracks = new List<Track>()
                    };

                    album.Discs.Add(disc);

                    await dbContext.SaveChangesAsync();

                    MediaType mediaType = await dbContext.MediaTypes.FirstOrDefaultAsync(mt => mt.Id == discVm.SelectedMediaType);

                    if (mediaType != null)
                    {
                        disc.MediaTypes.Add(mediaType);

                        await dbContext.SaveChangesAsync();
                    }

                    foreach (Track track1 in discVm.Tracks)
                    {
                        var trackVm = (TrackViewModel)track1;

                        var track = new Track
                        {
                            Title = trackVm.Title,
                            Length = trackVm.Length,
                            TrackNumber = trackVm.TrackNumber,
                            Notes = trackVm.Notes,
                            TrackPersonGroupPersonRelations = new List<TrackPersonGroupPersonRelation>()
                        };

                        if (trackVm.TrackStreamInfo != null)
                        {
                            track.TrackStreamInfo = new TrackStreamInfo
                            {
                                FilePath = trackVm.TrackStreamInfo.FilePath,
                                IncludeInAutoPlaylist = trackVm.TrackStreamInfo.IncludeInAutoPlaylist,
                                PlayCount = 0,
                                Weight = 100
                            };
                        }

                        disc.Tracks.Add(track);

                        await dbContext.SaveChangesAsync();

                        if (trackVm.TrackPersonGroupPersonRelations == null)
                        {
                            continue;
                        }

                        foreach (TrackPersonGroupPersonRelation trackVmPersonRelations in trackVm.TrackPersonGroupPersonRelations)
                        {
                            PersonGroup personGroup = await dbContext.PersonGroups.FirstOrDefaultAsync(pg => pg.Id == trackVmPersonRelations.PersonGroup.Id);

                            TrackPersonGroupPersonRelation trackPersonRelations = new()
                            {
                                PersonGroup = personGroup,
                                Persons = new List<Person>()
                            };
                            track.TrackPersonGroupPersonRelations.Add(trackPersonRelations);

                            await dbContext.SaveChangesAsync();

                            foreach (Person personVm in trackVmPersonRelations.Persons)
                            {
                                Person person = await dbContext.Persons
                                    .Where(p => p.FirstName == personVm.FirstName && p.LastName == personVm.LastName)
                                    .OrderBy(p => p.Version)
                                    .FirstOrDefaultAsync() ?? new Person
                                {
                                    FirstName = personVm.FirstName,
                                    LastName = personVm.LastName,
                                    Version = 0
                                };
                                trackPersonRelations.Persons.Add(person);

                                await dbContext.SaveChangesAsync();
                            }
                        }
                    }
                }
            }

            ImportInProgress = false;
            ImportComplete = true;

            ImporterState.AlbumsToImport = null;
            ImporterState.SelectedFiles = null;
        }

        private async Task OnBackClick()
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to go back? All tag info will be reread and any changes you've made to album/track names, artists, or reorderings will be lost.");
            if (confirmed)
            {
                NavigationManager.NavigateTo("/admin/mediaimporter/step3");
            }
        }
    }
}
