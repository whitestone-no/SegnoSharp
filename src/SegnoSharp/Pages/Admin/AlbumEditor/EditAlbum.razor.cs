using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Pages.Admin.AlbumEditor
{
    public partial class EditAlbum
    {
        [Parameter] public int Id { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        private Album Album { get; set; }
        private List<PersonGroup> PersonGroups { get; set; }
        private int SelectedPersonGroupId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            Album = await dbContext.Albums
                .Include(a => a.Genres)
                .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.Persons)
                .Include(a => a.AlbumPersonGroupPersonRelations).ThenInclude(r => r.PersonGroup)
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
    }
}
