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
using Whitestone.SegnoSharp.Models.ViewModels;

namespace Whitestone.SegnoSharp.Pages.Admin.AlbumEditor
{
    public partial class PersonGroups : IDisposable
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }

        private SegnoSharpDbContext DbContext { get; set; }
        private List<PersonGroupViewModel> DbGroups { get; set; } = new();

        private PersonGroupViewModel _currentlyDraggingGroup = null;
        private PersonGroupViewModel _currentlyDraggingOverGroup = null;

        protected override async Task OnInitializedAsync()
        {
            DbContext = await DbFactory.CreateDbContextAsync();

            DbGroups = await DbContext.PersonGroups
                .Include(p => p.PersonGroupStreamInfo)
                .Select(g => new PersonGroupViewModel(DbContext)
                {
                    Id = g.Id,
                    Name = g.Name,
                    SortOrder = g.SortOrder,
                    Type = g.Type,
                    PersonGroupStreamInfo = g.PersonGroupStreamInfo
                })
                .ToListAsync();
        }

        private async Task SaveChanges()
        {
            await DbContext.SaveChangesAsync();
        }

        private async Task DiscardChanges()
        {
            DbContext.ChangeTracker.Clear();

            DbGroups = await DbContext.PersonGroups
                .Include(p => p.PersonGroupStreamInfo)
                .Select(g => new PersonGroupViewModel(DbContext)
                {
                    Id = g.Id,
                    Name = g.Name,
                    SortOrder = g.SortOrder,
                    Type = g.Type,
                    PersonGroupStreamInfo = g.PersonGroupStreamInfo
                })
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

        private async Task RemoveGroup(PersonGroupViewModel group)
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete {group.Name}?");
            if (!confirmed)
            {
                return;
            }

            DbGroups.Remove(group);
            DbContext.PersonGroups.Remove(group);

            foreach (PersonGroupViewModel dbGroup in DbGroups.Where(g => g.Type == group.Type && g.SortOrder > group.SortOrder))
            {
                dbGroup.SortOrder = (ushort)(dbGroup.SortOrder - 1);
            }
        }

        private void HandleDragStart(PersonGroupViewModel group)
        {
            _currentlyDraggingGroup = group;
        }

        private void HandleDrop(PersonGroupViewModel targetGroup)
        {
            if (targetGroup.Type != _currentlyDraggingGroup.Type)
            {
                return;
            }

            ushort newSortOrder = targetGroup.SortOrder;
            ushort oldSortOrder = _currentlyDraggingGroup.SortOrder;

            // If moving "down"
            if (newSortOrder > oldSortOrder)
            {
                newSortOrder = (ushort)(newSortOrder - 1);
                foreach (PersonGroupViewModel moveGroup in DbGroups.Where(g => g.Type == targetGroup.Type && g.SortOrder <= newSortOrder && g.SortOrder > oldSortOrder && g.Id != _currentlyDraggingGroup.Id))
                {
                    moveGroup.SortOrder = (ushort)(moveGroup.SortOrder - 1);
                }
            }
            // If moving "up"
            else if (newSortOrder < oldSortOrder)
            {
                foreach (PersonGroupViewModel moveGroup in DbGroups.Where(g => g.Type == targetGroup.Type && g.SortOrder < oldSortOrder && g.SortOrder >= newSortOrder && g.Id != _currentlyDraggingGroup.Id))
                {
                    moveGroup.SortOrder = (ushort)(moveGroup.SortOrder + 1);
                }
            }

            _currentlyDraggingGroup.SortOrder = newSortOrder;
        }

        private void HandleDragEnd()
        {
            _currentlyDraggingGroup = null;
            _currentlyDraggingOverGroup = null;
        }

        private void HandleDragEnter(PersonGroupViewModel group)
        {
            if (group.Type == _currentlyDraggingGroup.Type)
            {
                _currentlyDraggingOverGroup = group;
            }
        }

        private void AddGroup(PersonGroupType type)
        {
            PersonGroupViewModel pg = new(DbContext)
            {
                Type = type,
                Name = "New group",
                SortOrder = (ushort)(DbGroups.Where(g => g.Type == type).Max(g => g.SortOrder) + 1),
            };

            DbGroups.Add(pg);
            DbContext.PersonGroups.Add(pg);
        }
    }
}
