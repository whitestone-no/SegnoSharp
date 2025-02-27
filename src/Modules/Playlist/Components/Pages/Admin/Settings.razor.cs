using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Modules.Playlist.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components.Pages.Admin
{
    public partial class Settings
    {
        [Inject] private IEnumerable<IPlaylistProcessor> PlaylistProcessors { get; set; }
        [Inject] private PlaylistSettings PlaylistSettings { get; set; }

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
