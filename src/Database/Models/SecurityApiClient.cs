using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Whitestone.SegnoSharp.Database.Models
{
    public class SecurityApiClient
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public bool Enabled { get; set; } = true;

        [Required]
        public DateTime Created { get; set; }

        public List<SecurityApiClientPermission> Permissions { get; set; } = [];
        public List<SecurityApiKey> Keys { get; set; } = [];
    }
}
