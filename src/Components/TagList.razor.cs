using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Whitestone.SegnoSharp.Database.Interfaces;

namespace Whitestone.SegnoSharp.Components
{
    public partial class TagList<TItem> where TItem : ITag, new()
    {
        [Parameter] public IList<TItem> Items { get; set; }
        [Parameter] public object Context { get; set; }

        [Parameter, EditorRequired] public Func<string, object, Task<IEnumerable<TItem>>> ExecuteSearch { get; set; }

        private string Search { get; set; } = string.Empty;
        private IEnumerable<TItem> SearchResults { get; set; } = new List<TItem>();

        private Timer _searchTimer;

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

            SearchResults = await ExecuteSearch(Search, Context);

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
    }
}
