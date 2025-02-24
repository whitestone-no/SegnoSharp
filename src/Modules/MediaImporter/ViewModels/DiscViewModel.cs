using System;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.ViewModels
{
    public class DiscViewModel : Disc
    {
        public int SelectedMediaType { get; set; }
        public Guid TempId { get; set; }
    }
}
