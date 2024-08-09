using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;

namespace Whitestone.SegnoSharp.Components.Pages.Admin
{
    public partial class Queue : IDisposable
    {
        [Inject] private IDbContextFactory<SegnoSharpDbContext> DbFactory { get; set; }

        public int GenerateAmount { get; set; }
        //public List<PlaylistMetadataView> GeneratedItems { get; set; } = [];

        private SegnoSharpDbContext DbContext { get; set; }

        protected override async Task OnInitializedAsync()
        {
            DbContext = await DbFactory.CreateDbContextAsync();
        }

        private void GenerateQueue()
        {
            //GeneratedItems = DbContext.PlaylistMetadataView
            //    .Where(p => p.Artist.Contains(14))
            //    .Take(20)
            //    .ToList();

            //for (var i = 0; i < GenerateAmount; i++)
            //{
            //    int totalWeight = DbContext.TrackStreamInfos.Where(t => t.IncludeInAutoPlaylist).Sum(t => t.Weight);

            //    var random = new Random();
            //    double randomWeight = random.NextDouble() * totalWeight;
            //}
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
