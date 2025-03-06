﻿using System;

namespace Whitestone.SegnoSharp.Common.Models
{
    public class Track
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string File { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
