using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Database.Interfaces;

namespace Whitestone.SegnoSharp.Modules.AlbumEditor.Components
{
    public partial class TagList<TItem> where TItem : ITag, new()
    {
        [Parameter] public IList<TItem> Items { get; set; }

        [Parameter] public Func<string, Task<IEnumerable<TItem>>> ExecuteSearch { get; set; }
        [Parameter] public IList<TItem> Selection { get; set; }

        private string Search { get; set; } = string.Empty;
        private IEnumerable<TItem> SearchResults { get; set; } = new List<TItem>();

        private int SelectionSelected
        {
            get => _selectionSelected;
            set
            {
                if (_selectionSelected == value)
                {
                    return;
                }
                _selectionSelected = value;
                AddItem(value);
            }
        }

        private Timer _searchTimer;
        private int _selectionSelected;

        private void StartSearchTimer()
        {
            DisposeSearchTimer();
            _searchTimer = new Timer(300);
            _searchTimer.Elapsed += SearchTimerElapsedTickAsync;
            _searchTimer.Enabled = true;
            _searchTimer.Start();
        }

        private void DisposeSearchTimer()
        {
            if (_searchTimer == null)
            {
                return;
            }

            _searchTimer.Enabled = false;
            _searchTimer.Elapsed -= SearchTimerElapsedTickAsync;
            _searchTimer.Dispose();
            _searchTimer = null;
        }

        private async void SearchTimerElapsedTickAsync(object sender, EventArgs e)
        {
            DisposeSearchTimer();

            if (string.IsNullOrEmpty(Search))
            {
                SearchResults = new List<TItem>();
                await InvokeAsync(StateHasChanged);
                return;
            }

            IEnumerable<TItem> searchResults = await ExecuteSearch(Search);

            SearchResults = searchResults.Where(r => !Items.Contains(r));

            await InvokeAsync(StateHasChanged);
        }

        private void AddItem(TItem item)
        {
            Items.Add(item);
            Search = string.Empty;
            SearchResults = new List<TItem>();
        }

        private void AddNewItem(string newName)
        {
            TItem item = new()
            {
                TagName = newName
            };

            AddItem(item);
        }

        private void RemoveItem(TItem item)
        {
            Items.Remove(item);
        }

        private void OnBlur()
        {
            Search = string.Empty;
            SearchResults = new List<TItem>();
        }

        private void AddItem(int id)
        {
            if (id == 0)
            {
                return;
            }

            Items.Add(Selection.First(s => s.Id == id));
            SelectionSelected = 0;
        }
    }
}
