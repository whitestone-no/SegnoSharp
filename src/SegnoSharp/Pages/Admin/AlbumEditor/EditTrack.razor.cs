using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Pages.Admin.AlbumEditor
{
    public partial class EditTrack : IDisposable
    {
        [Parameter] public int Id { get; set; }
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        private SegnoSharpDbContext DbContext { get; set; }
        private Track Track { get; set; }
        private List<PersonGroup> PersonGroups { get; set; }

        protected override async Task OnInitializedAsync()
        {
            DbContext = await DbFactory.CreateDbContextAsync();

            Track = await DbContext.Tracks
                .Include(t => t.Disc).ThenInclude(d => d.Album)
                .FirstOrDefaultAsync(t => t.Id == Id);

            PersonGroups = await DbContext.PersonGroups
                .Where(g => g.Type == PersonGroupType.Album)
                .ToListAsync();
        }

        private async Task Close(bool save = false)
        {
            if (save)
            {
                await DbContext.SaveChangesAsync();
            }

            NavigationManager.NavigateTo($"/admin/albums/{Track.Disc.Album.Id}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DbContext?.Dispose();
            }
        }
    }
}
