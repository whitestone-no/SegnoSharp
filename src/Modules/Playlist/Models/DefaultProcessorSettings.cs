﻿using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;
using Whitestone.SegnoSharp.Shared.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Models
{
    public class DefaultProcessorSettings : PlaylistProcessorSettings
    {
        [Persist]
        [DefaultValue(true)]
        public override bool Enabled { get; set; }

        [Persist]
        [DefaultValue(false)]
        [Description("Use a weighted random selection algorithm to select the next track.")]
        public bool UseWeightedRandom { get; set; }
    }
}
