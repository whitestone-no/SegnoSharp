using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Modules.Playlist.ViewModels;
using System.Linq;
using Whitestone.SegnoSharp.Database.Models;
using System;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Pages.Admin
{
    public partial class Playlist
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        public SearchViewModel SearchModel { get; set; } = new();
        public List<SearchResultViewModel> SearchResults { get; set; } = [];

        private const int SearchResultPageSize = 20;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        private async Task DoSearchAsync()
        {
            if (string.IsNullOrEmpty(SearchModel.SearchTerm))
            {
                return;
            }

            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            SearchResults = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(tsi => 
                    // Public/AutoPlaylist filters
                    (!SearchModel.OnlyPublic || (SearchModel.OnlyPublic && tsi.Track.Disc.Album.IsPublic)) &&
                    (!SearchModel.OnlyAutoPlaylist || (SearchModel.OnlyAutoPlaylist && tsi.IncludeInAutoPlaylist)) &&
                    
                    // Search criteria
                    (
                        (SearchModel.SearchForFilename && 
                            EF.Functions.Like(tsi.FilePath, "%" + SearchModel.SearchTerm + "%")) ||
                        
                        (SearchModel.SearchForAlbum && 
                            EF.Functions.Like(tsi.Track.Disc.Album.Title, "%" + SearchModel.SearchTerm + "%")) ||
                        
                        (SearchModel.SearchForTrack && 
                            EF.Functions.Like(tsi.Track.Title, "%" + SearchModel.SearchTerm + "%")) ||
                        
                        (SearchModel.SearchForArtist && 
                            (
                                // Search in track artists
                                (tsi.Track.TrackPersonGroupPersonRelations.Any(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                                    r.Persons.Any(p =>
                                        EF.Functions.Like(p.FirstName, "%" + SearchModel.SearchTerm + "%") ||
                                        EF.Functions.Like(p.LastName, "%" + SearchModel.SearchTerm + "%")
                                    )
                                )) ||
                                // Search in album artists
                                (tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations.Any(r =>
                                    r.PersonGroup.PersonGroupStreamInfo != null &&
                                    r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist &&
                                    r.Persons.Any(p =>
                                        EF.Functions.Like(p.FirstName, "%" + SearchModel.SearchTerm + "%") ||
                                        EF.Functions.Like(p.LastName, "%" + SearchModel.SearchTerm + "%")
                                    )
                                ))
                            )
                        )
                    )
                )
                .Take(SearchResultPageSize)
                .Select(tsi => new SearchResultViewModel
                {
                    AlbumTitle = tsi.Track.Disc.Album.Title,
                    TrackTitle = tsi.Track.Title,
                    Length = tsi.Track.Length,
                    TrackArtists = string.Join(", ", 
                        tsi.Track.TrackPersonGroupPersonRelations
                            .Where(r => 
                                r.PersonGroup.PersonGroupStreamInfo != null &&
                                r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                            .SelectMany(r => 
                                r.Persons.Select(p => 
                                    p.FirstName == null ? p.LastName : p.FirstName + " " + p.LastName))),
                    AlbumArtists = string.Join(", ", 
                        tsi.Track.Disc.Album.AlbumPersonGroupPersonRelations
                            .Where(r => 
                                r.PersonGroup.PersonGroupStreamInfo != null &&
                                r.PersonGroup.PersonGroupStreamInfo.IncludeInAutoPlaylist)
                            .SelectMany(r => 
                                r.Persons.Select(p => 
                                    p.FirstName == null ? p.LastName : p.FirstName + " " + p.LastName)))
                })
                .ToListAsync();
        }
    }
}
