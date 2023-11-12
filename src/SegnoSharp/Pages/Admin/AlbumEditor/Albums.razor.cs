using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Pages.Admin
{
    public partial class Albums
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        private List<Album> AlbumList { get; set; } = new();
        private SearchModel SearchModel { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            AlbumList = await dbContext.Albums
                .AsNoTracking()
                .Include(a => a.Discs).ThenInclude(d => d.Tracks)
                .AsSplitQuery()
                .OrderByDescending(a => a.Added)
                .Take(10)
                .ToListAsync();
        }

        private async Task DoSearch()
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            AlbumList = dbContext.Albums
                .AsNoTracking()
                .Include(a => a.Discs).ThenInclude(d => d.Tracks)
                .AsSplitQuery()
                .Where(a => EF.Functions.Like(a.Title, "%" + SearchModel.SearchString + "%"))
                .OrderBy(a => a.Title)
                .ToList();
        }

        private static string GetAlbumUrl(int id)
        {
            return $"/admin/albums/{id}";
        }
    }

    public class SearchModel
    {
        public string SearchString { get; set; }
    }
}
