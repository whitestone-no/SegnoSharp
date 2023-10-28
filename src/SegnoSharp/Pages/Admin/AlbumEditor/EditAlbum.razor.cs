using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Models.ViewModels;

namespace Whitestone.SegnoSharp.Pages.Admin.AlbumEditor
{
    public partial class EditAlbum
    {
        [Parameter] public int Id { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        private Album Album { get; set; }
        private List<PersonGroup> PersonGroups { get; set; }
        private int SelectedPersonGroupId { get; set; }
        private bool AlbumCoverFileSizeError { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            Album = await dbContext.Albums
                .Include(a => a.Genres)
                .Include(a => a.RecordLabels)
                .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.Persons)
                .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.PersonGroup)
                .Include(a => a.AlbumCover).ThenInclude(c => c.AlbumCoverData)
                .FirstOrDefaultAsync(a => a.Id == Id);

            PersonGroups = await dbContext.PersonGroups
                .Where(g => g.Type == PersonGroupType.Album)
                .ToListAsync();
        }

        private async Task<IEnumerable<Genre>> ExecuteGenreSearch(string searchTerm)
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            return await dbContext.Genres
                .Where(g => EF.Functions.Like(g.Name, "%" + searchTerm + "%") && !Album.Genres.Select(gg => gg.Id).Contains(g.Id))
                .ToListAsync();
        }

        private async Task<IEnumerable<Person>> ExecutePersonSearch(string searchTerm)
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            return await dbContext.Persons
                .Where(p => EF.Functions.Like(p.LastName, "%" + searchTerm + "%") || EF.Functions.Like(p.FirstName, "%" + searchTerm + "%"))
                .ToListAsync();
        }

        private async Task<IEnumerable<RecordLabel>> ExecuteRecordLabelSearch(string searchTerm)
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            return await dbContext.RecordLabels
                .Where(rl => EF.Functions.Like(rl.Name, "%" + searchTerm + "%"))
                .ToListAsync();
        }
        
        private void AddPersonGroup()
        {
            Album.AlbumPersonGroupPersonRelations.Add(new AlbumPersonGroupPersonRelation
            {
                PersonGroup = PersonGroups.First(pg => pg.Id == SelectedPersonGroupId)
            });
        }

        private void Save()
        {
            ;
        }

        private async Task AddAlbumCover(InputFileChangeEventArgs e)
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
                AlbumCoverFileSizeError = true;
                return;
            }

            AlbumCoverFileSizeError = false;

            byte[] fileBytes = ms.ToArray();

            Album.AlbumCover = new AlbumCover
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

        private void RemoveAlbumCover()
        {
            Album.AlbumCover = null;
        }
    }
}
