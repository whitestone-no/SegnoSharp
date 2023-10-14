using System.Collections.Generic;
using System.Linq;
using System;
using Whitestone.SegnoSharp.Database.Extensions;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Models.ViewModels
{
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
}
