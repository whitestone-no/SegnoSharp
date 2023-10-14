using System.Collections.Generic;
using System.Linq;
using System;
using Whitestone.SegnoSharp.Database.Extensions;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Models.ViewModels
{
    public class AlbumViewModel : Album
    {
        public int PersonGroupMappingId;
        public bool AlbumCoverFileSizeError;
        public Guid TempId { get; set; }
        public bool AlbumAlreadyExists { get; set; }

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
}
