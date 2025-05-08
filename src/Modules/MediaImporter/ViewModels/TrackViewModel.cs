using System;
using System.Collections.Generic;
using System.Linq;
using Whitestone.SegnoSharp.Database.Extensions;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.ViewModels
{
    public class TrackViewModel : Track
    {
        public int AlbumArtistPersonGroupMappingId;
        public int ArtistPersonGroupMappingId;
        public int ComposerPersonGroupMappingId;

        public TrackViewModel()
        {
            TrackPersonGroupPersonRelations = new List<TrackPersonGroupPersonRelation>();
        }

        public bool AutoPlaylistDisabled => TrackStreamInfo == null;

        public bool IncludeInAutoPlaylist
        {
            get => TrackStreamInfo is { IncludeInAutoPlaylist: true };
            set => TrackStreamInfo.IncludeInAutoPlaylist = value;
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

                if (persons.Count <= 0)
                {
                    TrackPersonGroupPersonRelation relation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ArtistPersonGroupMappingId);
                    if (relation != null)
                    {
                        TrackPersonGroupPersonRelations.Remove(relation);
                        return;
                    }
                }
                
                // If track artist is the same persons as album artist, then blank the track artists
                bool? sequenceEqual = Disc.Album.AlbumPersonGroupPersonRelations
                    .FirstOrDefault(r => r.PersonGroup.Id == AlbumArtistPersonGroupMappingId)?
                    .Persons
                    .OrderBy(x => x)
                    .SequenceEqual(persons.OrderBy(x => x));

                if (sequenceEqual == null || !sequenceEqual.Value)
                {
                    TrackPersonGroupPersonRelation relation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ArtistPersonGroupMappingId);
                    if (relation == null)
                    {
                        relation = new TrackPersonGroupPersonRelation
                        {
                            PersonGroup = new PersonGroup
                            {
                                Id = ArtistPersonGroupMappingId,
                                Type = PersonGroupType.Track
                            }
                        };
                        TrackPersonGroupPersonRelations.Add(relation);
                    }

                    relation.Persons = persons;
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

                if (persons.Count <= 0)
                {
                    TrackPersonGroupPersonRelation relation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ComposerPersonGroupMappingId);
                    if (relation != null)
                    {
                        TrackPersonGroupPersonRelations.Remove(relation);
                        return;
                    }
                }

                // If track composer is the same persons as album artist, then blank the track composers
                bool? sequenceEqual = Disc.Album.AlbumPersonGroupPersonRelations
                    .FirstOrDefault(r => r.PersonGroup.Id == AlbumArtistPersonGroupMappingId)?
                    .Persons
                    .OrderBy(x => x)
                    .SequenceEqual(persons.OrderBy(x => x));

                if (sequenceEqual == null || !sequenceEqual.Value)
                {
                    TrackPersonGroupPersonRelation relation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ComposerPersonGroupMappingId);

                    if (relation == null)
                    {
                        relation = new TrackPersonGroupPersonRelation
                        {
                            PersonGroup = new PersonGroup
                            {
                                Id = ComposerPersonGroupMappingId,
                                Type = PersonGroupType.Track
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
