using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Extensions;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models;
using Whitestone.SegnoSharp.Modules.MediaImporter.ViewModels;
using Track = Whitestone.SegnoSharp.Database.Models.Track;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.Components.Pages
{
    public partial class Step3
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ImportState ImporterState { get; set; }
        [Inject] private ITagReader TagReader { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private IOptions<TagReaderConfig> TagReaderConfig { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }

        private TrackViewModel _currentlyDraggingTrack;
        private TrackViewModel _currentlyDraggingOverTrack;

        private List<MediaType> MediaTypes { get; set; }

        private readonly char[] _nameSeparators = { ',', '&', '/', '\\', ';' };
        private bool _loaded;

        protected override async Task OnInitializedAsync()
        {
            if (ImporterState.SelectedFiles == null)
            {
                ImporterState.AlbumsToImport = null;
                _loaded = true;
                return;
            }

            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            MediaTypes = await dbContext.MediaTypes.ToListAsync();
            
            int albumArtistGroupId = TagReaderConfig.Value.TagMappings["AlbumArtist"];
            int trackArtistGroupId = TagReaderConfig.Value.TagMappings["Artist"];
            int trackComposerGroupId = TagReaderConfig.Value.TagMappings["Composer"];

            List<Tags> tags = ImporterState.SelectedFiles
                .Where(f => f.Import)
                .Select(file =>
                {
                    Tags tags = TagReader.ReadTagInfo(file.File.FullName);
                    return tags;
                })
                .ToList();

            ImporterState.AlbumsToImport = [];

            foreach (IGrouping<string, Tags> albumGroup in tags.GroupBy(a => a.Album))
            {
                var album = new AlbumViewModel
                {
                    Title = albumGroup.Key,
                    Published = albumGroup.Min(x => x.Year),
                    IsPublic = true,
                    Genres = new List<Genre>(),
                    Discs = new List<Disc>(),
                    TempId = Guid.NewGuid(),
                    PersonGroupMappingId = albumArtistGroupId,
                    AlbumAlreadyExists = await dbContext.Albums.AnyAsync(a => a.Title == albumGroup.Key)
                };

                Tags firstTagWithCover = albumGroup.FirstOrDefault(t => t.CoverImage != null);
                if (firstTagWithCover != null)
                {
                    album.AlbumCover = new AlbumCover
                    {
                        Filename = albumGroup.Key.ToFileName(firstTagWithCover.CoverImage.MimeType),
                        Filesize = Convert.ToUInt32(firstTagWithCover.CoverImage.Data.Length),
                        Mime = firstTagWithCover.CoverImage.MimeType,
                        AlbumCoverData = new AlbumCoverData
                        {
                            Data = firstTagWithCover.CoverImage.Data
                        }
                    };
                }

                List<Person> albumArtists = albumGroup
                    .Select(x => x.AlbumArtist)
                    .Distinct()
                    .SelectMany(aa =>
                    {
                        string[] names = aa.Split(_nameSeparators, StringSplitOptions.TrimEntries & StringSplitOptions.RemoveEmptyEntries);
                        return names.Select(n =>
                        {
                            n = n.Trim();
                            string lastname = n;
                            string firstname = null;

                            int lastSpaceIndex = n.LastIndexOf(' ');
                            if (lastSpaceIndex != -1)
                            {
                                firstname = n[..lastSpaceIndex].Trim();
                                lastname = n[lastSpaceIndex..].Trim();
                            }

                            return new Person
                            {
                                FirstName = firstname,
                                LastName = lastname,
                            };
                        });
                    })
                    .Distinct()
                    .ToList();

                album.AlbumPersonGroupPersonRelations = new List<AlbumPersonGroupPersonRelation>
                {
                    new()
                    {
                        Parent = album,
                        PersonGroup = new PersonGroup
                        {
                            Id = albumArtistGroupId
                        },
                        Persons = albumArtists
                    }
                };

                foreach (string fileGenre in albumGroup.Select(x => x.Genre).Distinct().OrderBy(x => x))
                {
                    var genre = new Genre
                    {
                        Name = fileGenre
                    };

                    album.Genres.Add(genre);
                }

                ImporterState.AlbumsToImport.Add(album);

                foreach (IGrouping<byte, Tags> discGroup in albumGroup.GroupBy(x => x.Disc))
                {
                    var disc = new DiscViewModel
                    {
                        DiscNumber = discGroup.Key,
                        Tracks = new List<Track>(),
                        Album = album,
                        TempId = Guid.NewGuid()
                    };

                    album.Discs.Add(disc);

                    foreach (Tags fileTrack in discGroup.OrderBy(t => t.TrackNumber))
                    {
                        ushort length;
                        try
                        {
                            length = Convert.ToUInt16(fileTrack.Duration);
                        }
                        catch
                        {
                            length = ushort.MaxValue;
                        }

                        var track = new TrackViewModel
                        {
                            Title = fileTrack.Title,
                            TrackNumber = fileTrack.TrackNumber,
                            Length = length,
                            Disc = disc,
                            ArtistPersonGroupMappingId = trackArtistGroupId,
                            ComposerPersonGroupMappingId = trackComposerGroupId,
                            AlbumArtistPersonGroupMappingId = albumArtistGroupId
                        };

                        bool importToPlaylist = ImporterState.SelectedFiles.FirstOrDefault(f => f.File.FullName == fileTrack.File)?.ImportToPlaylist ?? false;
                        if (importToPlaylist)
                        {
                            track.TrackStreamInfo = new TrackStreamInfo
                            {
                                FilePath = fileTrack.File,
                                IncludeInAutoPlaylist = true,
                                Track = track
                            };
                        }

                        if (!string.IsNullOrEmpty(fileTrack.Artist))
                        {
                            string[] names = fileTrack.Artist.Split(_nameSeparators, StringSplitOptions.TrimEntries & StringSplitOptions.RemoveEmptyEntries);
                            List<Person> trackArtists = names.Select(n =>
                                {
                                    n = n.Trim();
                                    string lastname = n;
                                    string firstname = null;

                                    int lastSpaceIndex = n.LastIndexOf(' ');
                                    if (lastSpaceIndex != -1)
                                    {
                                        firstname = n[..lastSpaceIndex].Trim();
                                        lastname = n[lastSpaceIndex..].Trim();
                                    }

                                    return new Person
                                    {
                                        FirstName = firstname,
                                        LastName = lastname,
                                    };
                                })
                                .Distinct()
                                .ToList();

                            if (!trackArtists.OrderBy(x => x).SequenceEqual(albumArtists.OrderBy(x => x)))
                            {
                                track.TrackPersonGroupPersonRelations = new List<TrackPersonGroupPersonRelation>
                                {
                                    new()
                                    {
                                        Parent = track,
                                        PersonGroup = new PersonGroup
                                        {
                                            Id = trackArtistGroupId
                                        },
                                        Persons = trackArtists
                                    }
                                };
                            }
                        }

                        if (!string.IsNullOrEmpty(fileTrack.Composer))
                        {
                            string[] names = fileTrack.Composer.Split(_nameSeparators, StringSplitOptions.TrimEntries & StringSplitOptions.RemoveEmptyEntries);
                            List<Person> trackComposers = names.Select(n =>
                            {
                                n = n.Trim();
                                string lastname = n;
                                string firstname = null;

                                int lastSpaceIndex = n.LastIndexOf(' ');
                                if (lastSpaceIndex != -1)
                                {
                                    firstname = n[..lastSpaceIndex].Trim();
                                    lastname = n[lastSpaceIndex..].Trim();
                                }

                                return new Person
                                {
                                    FirstName = firstname,
                                    LastName = lastname,
                                };
                            })
                                .Distinct()
                                .ToList();

                            if (!trackComposers.OrderBy(x => x).SequenceEqual(albumArtists.OrderBy(x => x)))
                            {
                                track.TrackPersonGroupPersonRelations = new List<TrackPersonGroupPersonRelation>
                                {
                                    new()
                                    {
                                        Parent = track,
                                        PersonGroup = new PersonGroup
                                        {
                                            Id = trackComposerGroupId
                                        },
                                        Persons = trackComposers
                                    }
                                };
                            }
                        }

                        disc.Tracks.Add(track);
                    }
                }
            }

            _loaded = true;
        }

        private async Task AlbumNameChanged(AlbumViewModel album)
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            album.AlbumAlreadyExists = await dbContext.Albums.AnyAsync(a => a.Title == album.Title);
        }

        private async Task CreateAlbumNameCopy(AlbumViewModel album)
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            var iterator = 1;

            do
            {
                string albumCopyName = album.Title + " - Copy " + iterator;

                if (await dbContext.Albums.AnyAsync(a => a.Title == albumCopyName))
                {
                    iterator++;
                }
                else
                {
                    album.Title = albumCopyName;
                    album.AlbumAlreadyExists = false;
                    iterator = 0;
                }

            } while (iterator > 0);
        }

        private static void OnAlbumCoverRemoveClick(Album album)
        {
            album.AlbumCover = null;
        }

        private static async Task LoadAlbumCover(InputFileChangeEventArgs e, Album album)
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
                ((AlbumViewModel)album).AlbumCoverFileSizeError = true;
                return;
            }

            byte[] fileBytes = ms.ToArray();

            album.AlbumCover = new AlbumCover
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

        public void TrackNumberChanged(Track track, int oldTrackNumber)
        {
            if (track.TrackNumber > track.Disc.Tracks.Count)
            {
                track.TrackNumber = (ushort)track.Disc.Tracks.Count;
            }
            else if (track.TrackNumber <= 0)
            {
                track.TrackNumber = 1;
            }

            if (track.TrackNumber < oldTrackNumber) // Moving track up
            {
                foreach (Track existingTrack in track.Disc.Tracks.Where(t => t.TrackNumber >= track.TrackNumber && t.TrackNumber < oldTrackNumber && t != track))
                {
                    existingTrack.TrackNumber++;
                }
            }
            else if (track.TrackNumber > oldTrackNumber) // Moving track down
            {
                foreach (Track existingTrack in track.Disc.Tracks.Where(t => t.TrackNumber > oldTrackNumber && t.TrackNumber <= track.TrackNumber && t != track))
                {
                    existingTrack.TrackNumber--;
                }
            }
        }
        private void OnDragStart(TrackViewModel track)
        {
            _currentlyDraggingTrack = track;
        }

        private void HandleDrop(TrackViewModel targetTrack)
        {
            ushort newTrackNumber = targetTrack.TrackNumber;
            ushort oldTrackNumber = _currentlyDraggingTrack.TrackNumber;

            // If dragging track to another disc
            if (((DiscViewModel)_currentlyDraggingTrack.Disc).TempId != ((DiscViewModel)targetTrack.Disc).TempId)
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

                    foreach (Track moveTrack in targetTrack.Disc.Tracks.Where(t => t.TrackNumber <= newTrackNumber && t.TrackNumber > oldTrackNumber))
                    {
                        moveTrack.TrackNumber = (ushort)(moveTrack.TrackNumber - 1);
                    }

                }
                // If moving "up"
                else if (newTrackNumber < oldTrackNumber)
                {
                    foreach (Track moveTrack in targetTrack.Disc.Tracks.Where(t => t.TrackNumber < oldTrackNumber && t.TrackNumber >= newTrackNumber))
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
            _currentlyDraggingOverTrack = null;
        }

        private void HandleDragEnter(TrackViewModel track)
        {
            _currentlyDraggingOverTrack = track;
        }

        private void OnNextClick()
        {
            NavigationManager.NavigateTo("/admin/mediaimporter/step4");
        }

        private async Task OnBackClick()
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to go back? Any changes you've made to album/track names, artists, or reorderings will be lost");
            if (confirmed)
            {
                NavigationManager.NavigateTo("/admin/mediaimporter/step2");
            }
        }
    }
}
