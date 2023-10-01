using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
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
        [Inject] private SegnoSharpDbContext DbContext { get; set; }
        [Inject] private IOptions<TagReaderConfig> TagReaderConfig { get; set; }

        private List<Album> Albums { get; } = new();
        private TrackViewModel _currentlyDraggingTrack;
        private List<MediaType> MediaTypes => DbContext.MediaTypes.ToList();

        private readonly char[] _nameSeparators = new[] { ',', '&', '/', '\\' };

        protected override void OnInitialized()
        {
            if (ImporterState.SelectedFiles == null)
            {
                return;
            }

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
                        Album = album,
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
                            ImportToPlaylist = ImporterState.SelectedFiles.FirstOrDefault(f => f.File.FullName == fileTrack.File)?.ImportToPlaylist ?? true,
                            ArtistPersonGroupMappingId = trackArtistGroupId,
                            ComposerPersonGroupMappingId = trackComposerGroupId,
                            AlbumArtistPersonGroupMappingId = albumArtistGroupId
                        };

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
                                        Track = track,
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
                                        Track = track,
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
    }

    public class TrackViewModel : Track
    {
        public int AlbumArtistPersonGroupMappingId;
        public int ArtistPersonGroupMappingId;
        public int ComposerPersonGroupMappingId;

        public bool ImportToPlaylist { get; set; }
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
            get => string.Join(", ", TrackPersonGroupPersonRelations?
                .FirstOrDefault(r => r.PersonGroup.Id == ArtistPersonGroupMappingId)?
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
                                Track = this,
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
            get => string.Join(", ", TrackPersonGroupPersonRelations?
                .FirstOrDefault(r => r.PersonGroup.Id == ComposerPersonGroupMappingId)?
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
                                Track = this,
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
        public Guid TempId { get; set; }

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
            get => string.Join(", ", AlbumPersonGroupPersonRelations
                .FirstOrDefault(r => r.PersonGroup.Id == PersonGroupMappingId)?
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
                        Album = this,
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
}
