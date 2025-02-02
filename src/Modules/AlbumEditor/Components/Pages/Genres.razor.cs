using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.AlbumEditor.Components.Pages
{
    public partial class Genres : IDisposable
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }

        private SegnoSharpDbContext DbContext { get; set; }
        private List<Genre> DbGenres { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            DbContext = await DbFactory.CreateDbContextAsync();

            DbGenres = await DbContext.Genres
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        private async Task SaveChanges()
        {
            await DbContext.SaveChangesAsync();
        }

        private async Task DiscardChanges()
        {
            DbContext.ChangeTracker.Clear();

            DbGenres = await DbContext.Genres
                .OrderBy(g => g.Name)
                .ToListAsync();
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

        private async Task OnBeforeInternalNavigation(LocationChangingContext context)
        {
            if (DbContext.ChangeTracker.HasChanges())
            {
                var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "You have unsaved changes. Are you sure you want to leve this page?");
                if (!confirmed)
                {
                    context.PreventNavigation();
                }
            }
        }

        private async Task RemoveGenre(Genre genre)
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete {genre.Name}?");
            if (!confirmed)
            {
                return;
            }

            DbGenres.Remove(genre);
            DbContext.Genres.Remove(genre);
        }

        private void AddGenre()
        {
            Genre genre = new()
            {
                Name = "New Genre"
            };

            DbGenres.Add(genre);
            DbContext.Genres.Add(genre);
        }
    }
}
