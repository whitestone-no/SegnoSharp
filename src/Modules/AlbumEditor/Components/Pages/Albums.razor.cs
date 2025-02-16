using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.AlbumEditor.Components.Pages
{
    public partial class Albums
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        private List<Album> AlbumList { get; set; } = [];
        private SearchModel SearchModel { get; } = new();

        private const int PageSize = 10;
        private int TotalPages { get; set; }
        private int CurrentPage { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            TotalPages = (int)Math.Ceiling(await dbContext.Albums
                .AsNoTracking()
                .CountAsync() / (double)PageSize);

            AlbumList = await dbContext.Albums
                .AsNoTracking()
                .Include(a => a.Discs).ThenInclude(d => d.Tracks)
                .AsSplitQuery()
                .OrderByDescending(a => a.Added)
                .Skip(PageSize * (CurrentPage - 1))
                .Take(PageSize)
                .ToListAsync();
        }

        private async Task DoSearch()
        {
            CurrentPage = 1;

            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            TotalPages = (int)Math.Ceiling(await dbContext.Albums
                .AsNoTracking()
                .Where(a => EF.Functions.Like(a.Title, "%" + SearchModel.SearchString + "%"))
                .CountAsync() / (double)PageSize);

            AlbumList = await dbContext.Albums
                .AsNoTracking()
                .Include(a => a.Discs).ThenInclude(d => d.Tracks)
                .AsSplitQuery()
                .Where(a => EF.Functions.Like(a.Title, "%" + SearchModel.SearchString + "%"))
                .OrderByDescending(a => a.Added)
                .Skip(PageSize * (CurrentPage - 1))
                .Take(PageSize)
                .ToListAsync();
        }

        private async Task OnPageChanged(int page)
        {
            CurrentPage = page;

            await using SegnoSharpDbContext dbContext = await DbFactory.CreateDbContextAsync();

            AlbumList = await dbContext.Albums
                .AsNoTracking()
                .Include(a => a.Discs).ThenInclude(d => d.Tracks)
                .AsSplitQuery()
                .Where(a => EF.Functions.Like(a.Title, "%" + SearchModel.SearchString + "%"))
                .OrderByDescending(a => a.Added)
                .Skip(PageSize * (CurrentPage - 1))
                .Take(PageSize)
                .ToListAsync();
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
