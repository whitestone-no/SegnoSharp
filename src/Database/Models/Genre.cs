﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(Name))]
    public class Genre
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        public IList<Album> Albums { get; set; }
    }
}
