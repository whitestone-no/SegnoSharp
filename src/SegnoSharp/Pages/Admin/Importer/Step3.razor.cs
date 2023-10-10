using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Models.States;
using Track = Whitestone.SegnoSharp.Database.Models.Track;

namespace Whitestone.SegnoSharp.Pages.Admin.Importer
{
    public partial class Step3
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ImportState ImporterState { get; set; }
        [Inject] private ITagReader TagReader { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private IOptions<TagReaderConfig> TagReaderConfig { get; set; }

        private List<Album> Albums { get; } = new();
        private TrackViewModel _currentlyDraggingTrack;

        private List<MediaType> MediaTypes { get; set; }

        private readonly char[] _nameSeparators = new[] { ',', '&', '/', '\\', ';' };

        protected override void OnInitialized()
        {
            if (ImporterState.SelectedFiles == null)
            {
                return;
            }

            using SegnoSharpDbContext dbContext = DbFactory.CreateDbContext();

            MediaTypes = dbContext.MediaTypes.ToList();

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
                    PersonGroupMappingId = albumArtistGroupId
                };

                Tags firstTagWithCover = albumGroup.FirstOrDefault(t => t.CoverImage != null);
                if (firstTagWithCover != null)
                {
                    album.AlbumCover = new AlbumCover
                    {
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

                Albums.Add(album);

                foreach (IGrouping<byte, Tags> discGroup in albumGroup.GroupBy(x => x.Disc))
                {
                    var disc = new DiscViewModel()
                    {
                        DiscNumber = discGroup.Key,
                        Tracks = new List<Track>(),
                        Album = album
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

            base.OnInitialized();
        }

        private void OnNextClick()
        {
            NavigationManager.NavigateTo("/admin/import/step-4");
        }

        private void OnBackClick()
        {
            NavigationManager.NavigateTo("/admin/import/step-2");
        }

        private void OnDragStart(TrackViewModel track)
        {
            _currentlyDraggingTrack = track;
        }

        private void HandleDrop(TrackViewModel targetTrack)
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
            targetTrack.CssClass = string.Empty;
        }

        private void HandleDragEnd()
        {
            _currentlyDraggingTrack = null;
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
    }

    public class TrackViewModel : Track
    {
        public int AlbumArtistPersonGroupMappingId;
        public int ArtistPersonGroupMappingId;
        public int ComposerPersonGroupMappingId;

        public bool AutoPlaylistDisabled => TrackStreamInfo == null;

        public bool IncludeInAutoPlaylist
        {
            get => TrackStreamInfo is { IncludeInAutoPlaylist: true };
            set => TrackStreamInfo.IncludeInAutoPlaylist = value;
        }

        public string CssClass { get; set; } = string.Empty;

        public void HandleDragEnter()
        {
            CssClass = "drag-on";
        }
        public void HandleDragLeave()
        {
            CssClass = string.Empty;
        }

        public string ArtistString
        {
            get => TrackPersonGroupPersonRelations.GetNameString(ArtistPersonGroupMappingId);
            set
            {
                string[] names = value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                List<Person> persons = names.Select(n =>
                {
                    string lastname = n.Trim();
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
                        LastName = lastname
                    };
                }).ToList();

                // If track artist is the same persons as album artist, then blank the track artists
                bool? sequenceEqual = Disc.Album.AlbumPersonGroupPersonRelations
                    .FirstOrDefault(r => r.PersonGroup.Id == AlbumArtistPersonGroupMappingId)?
                    .Persons
                    .OrderBy(x => x)
                    .SequenceEqual(persons.OrderBy(x => x));

                if (sequenceEqual == null || !sequenceEqual.Value)
                {
                    if (TrackPersonGroupPersonRelations == null)
                    {
                        TrackPersonGroupPersonRelations = new List<TrackPersonGroupPersonRelation>
                        {
                            new()
                            {
                                Parent = this,
                                PersonGroup = new PersonGroup
                                {
                                    Id = ArtistPersonGroupMappingId
                                },
                                Persons = persons
                            }
                        };
                    }
                    else
                    {
                        TrackPersonGroupPersonRelation relation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ArtistPersonGroupMappingId);
                        if (relation == null)
                        {
                            relation = new TrackPersonGroupPersonRelation
                            {
                                PersonGroup = new PersonGroup
                                {
                                    Id = ComposerPersonGroupMappingId
                                }
                            };
                            TrackPersonGroupPersonRelations.Add(relation);
                        }

                        relation.Persons = persons;
                    }
                }
            }
        }

        public string ComposerString
        {
            get => TrackPersonGroupPersonRelations.GetNameString(ComposerPersonGroupMappingId);
            set
            {
                string[] names = value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                List<Person> persons = names.Select(n =>
                {
                    string lastname = n.Trim();
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
                        LastName = lastname
                    };
                }).ToList();

                // If track composer is the same persons as album artist, then blank the track composers
                bool? sequenceEqual = Disc.Album.AlbumPersonGroupPersonRelations
                    .FirstOrDefault(r => r.PersonGroup.Id == AlbumArtistPersonGroupMappingId)?
                    .Persons
                    .OrderBy(x => x)
                    .SequenceEqual(persons.OrderBy(x => x));

                if (sequenceEqual == null || !sequenceEqual.Value)
                {
                    if (TrackPersonGroupPersonRelations == null)
                    {
                        TrackPersonGroupPersonRelations = new List<TrackPersonGroupPersonRelation>
                        {
                            new()
                            {
                                Parent = this,
                                PersonGroup = new PersonGroup
                                {
                                    Id = ComposerPersonGroupMappingId
                                },
                                Persons = persons
                            }
                        };
                    }
                    else
                    {
                        TrackPersonGroupPersonRelation relation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ComposerPersonGroupMappingId);
                        if (relation == null)
                        {
                            relation = new TrackPersonGroupPersonRelation
                            {
                                PersonGroup = new PersonGroup
                                {
                                    Id = ComposerPersonGroupMappingId
                                }
                            };
                            TrackPersonGroupPersonRelations.Add(relation);
                        }

                        relation.Persons = persons;
                    }
                }
            }
        }
    }

    public class DiscViewModel : Disc
    {
        public int SelectedMediaType { get; set; }
    }

    public class AlbumViewModel : Album
    {
        public int PersonGroupMappingId;
        public bool AlbumCoverFileSizeError;
        public Guid TempId { get; set; }

        public string CoverImage
        {
            get
            {
                if (AlbumCover == null)
                {
                    return null;
                }

                return $"data:{AlbumCover.Mime};base64,{Convert.ToBase64String(AlbumCover.AlbumCoverData.Data)}";
            }
        }

        public string GenresString
        {
            get => string.Join(", ", Genres.Select(g => g.Name));
            set
            {
                string[] genres = value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                Genres = new List<Genre>();
                foreach (string genre in genres)
                {
                    Genres.Add(new Genre
                    {
                        Name = genre.Trim()
                    });
                }
            }
        }

        public string AlbumArtistString
        {
            get => AlbumPersonGroupPersonRelations.GetNameString(PersonGroupMappingId);
            set
            {
                string[] names = value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                List<Person> persons = names.Select(n =>
                    {
                        string lastname = n.Trim();
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
                            LastName = lastname
                        };
                    }).ToList();

                AlbumPersonGroupPersonRelations = new List<AlbumPersonGroupPersonRelation>
                {
                    new()
                    {
                        Parent = this,
                        PersonGroup = new PersonGroup
                        {
                            Id = PersonGroupMappingId
                        },
                        Persons = persons
                    }
                };
            }
        }
    }

    public static class PersonHelperExtensions
    {
        public static string GetNameString<TParent>(this IEnumerable<BasePersonGroupPersonRelation<TParent>> relations, int groupId)
        {
            if (relations == null)
            {
                return null;
            }

            string names = string.Join(", ", relations
                .FirstOrDefault(r => r.PersonGroup.Id == groupId)?
                .Persons
                .Select(p =>
                {
                    string name = p.LastName;
                    if (p.FirstName != null)
                    {
                        name = p.FirstName + " " + p.LastName;
                    }

                    return name;
                })
                .ToList() ?? new List<string>());

            return string.IsNullOrEmpty(names) ? null : names;
        }
    }
}
