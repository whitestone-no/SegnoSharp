using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Common.Interfaces;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Pages.Admin
{
    public partial class Processors
    {
        [Inject] private IEnumerable<IPlaylistProcessor> PlaylistProcessors { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        private void MoveProcessorUp(IPlaylistProcessor processor)
        {
            IPlaylistProcessor processorAbove = PlaylistProcessors.FirstOrDefault(p => p.Settings.SortOrder == processor.Settings.SortOrder + 1);
            
            if (processorAbove == null)
            {
                return;
            }

            processorAbove.Settings.SortOrder = (ushort)(processorAbove.Settings.SortOrder - 1);
            processor.Settings.SortOrder = (ushort)(processor.Settings.SortOrder + 1);
        }

        private void MoveProcessorDown(IPlaylistProcessor processor)
        {
            IPlaylistProcessor processorBelow = PlaylistProcessors.FirstOrDefault(p => p.Settings.SortOrder == processor.Settings.SortOrder - 1);

            if (processorBelow == null)
            {
                return;
            }

            processorBelow.Settings.SortOrder = (ushort)(processorBelow.Settings.SortOrder + 1);
            processor.Settings.SortOrder = (ushort)(processor.Settings.SortOrder - 1);
        }
    }
}
