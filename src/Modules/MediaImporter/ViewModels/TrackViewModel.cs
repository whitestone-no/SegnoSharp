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

                TrackPersonGroupPersonRelation relation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ArtistPersonGroupMappingId);

                if (relation != null && persons.Count <= 0)
                {
                    TrackPersonGroupPersonRelations.Remove(relation);
                    return;
                }

                // If track artist is the same persons as album artist, then blank the track artists
                // But only if the composer is empty
                bool? sequenceEqual = Disc.Album.AlbumPersonGroupPersonRelations
                    .FirstOrDefault(r => r.PersonGroup.Id == AlbumArtistPersonGroupMappingId)?
                    .Persons
                    .OrderBy(x => x)
                    .SequenceEqual(persons.OrderBy(x => x));

                TrackPersonGroupPersonRelation composerRelation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ComposerPersonGroupMappingId);

                if (sequenceEqual != null && sequenceEqual.Value && (composerRelation == null || composerRelation.Persons.Count <= 0))
                {
                    TrackPersonGroupPersonRelations.Remove(relation);
                    return;
                }

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

                TrackPersonGroupPersonRelation relation = TrackPersonGroupPersonRelations.FirstOrDefault(r => r.PersonGroup.Id == ComposerPersonGroupMappingId);

                if (relation != null && persons.Count <= 0)
                {
                    TrackPersonGroupPersonRelations.Remove(relation);
                    return;
                }

                // Don't do album artist comparison here as it should only be done for artist, not composer

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
