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
    public partial class MediaTypes : IDisposable
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }

        private SegnoSharpDbContext DbContext { get; set; }
        private List<MediaType> DbTypes { get; set; } = [];

        private MediaType _currentlyDraggingType;
        private MediaType _currentlyDraggingOverType;

        protected override async Task OnInitializedAsync()
        {
            DbContext = await DbFactory.CreateDbContextAsync();

            DbTypes = await DbContext.MediaTypes
                .ToListAsync();
        }

        private async Task SaveChanges()
        {
            await DbContext.SaveChangesAsync();
        }

        private async Task DiscardChanges()
        {
            DbContext.ChangeTracker.Clear();

            DbTypes = await DbContext.MediaTypes
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

        private async Task RemoveType(MediaType type)
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete {type.Name}?");
            if (!confirmed)
            {
                return;
            }

            DbTypes.Remove(type);
            DbContext.MediaTypes.Remove(type);
        }

        private void HandleDragStart(MediaType type)
        {
            _currentlyDraggingType = type;
        }

        private void HandleDrop(MediaType targetType)
        {
            byte newSortOrder = targetType.SortOrder;
            byte oldSortOrder = _currentlyDraggingType.SortOrder;

            // If moving "down"
            if (newSortOrder > oldSortOrder)
            {
                newSortOrder = (byte)(newSortOrder - 1);
                foreach (MediaType moveType in DbTypes.Where(t => t.SortOrder <= newSortOrder && t.SortOrder > oldSortOrder && t.Id != _currentlyDraggingType.Id))
                {
                    moveType.SortOrder = (byte)(moveType.SortOrder - 1);
                }
            }
            // If moving "up"
            else if (newSortOrder < oldSortOrder)
            {
                foreach (MediaType moveType in DbTypes.Where(t => t.SortOrder < oldSortOrder && t.SortOrder >= newSortOrder && t.Id != _currentlyDraggingType.Id))
                {
                    moveType.SortOrder = (byte)(moveType.SortOrder + 1);
                }
            }

            _currentlyDraggingType.SortOrder = newSortOrder;
        }

        private void HandleDragEnd()
        {
            _currentlyDraggingType = null;
            _currentlyDraggingOverType = null;
        }

        private void HandleDragEnter(MediaType type)
        {
            _currentlyDraggingOverType = type;
        }

        private void AddType()
        {
            MediaType mediaType = new()
            {
                Name = "New Media Type",
                SortOrder = (byte)(DbTypes.Max(t => t.SortOrder) + 1)
            };

            DbTypes.Add(mediaType);
            DbContext.MediaTypes.Add(mediaType);
        }
    }
}
