using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(Prefix), IsUnique = true)]
    public class SecurityApiKey
    {
        public int Id { get; set; }

        [Required]
        public int SecurityApiClientId { get; set; }

        [Required]
        public string Prefix { get; set; } = "";

        [Required]
        public byte[] Hash { get; set; } = []; // SHA-256 of the secret part
        
        
        public string Description { get; set; }

        [Required]
        public DateTime Created { get; set; }
        
        public DateTime? Expires { get; set; }
        public DateTime? Revoked { get; set; }
        public DateTime? LastUsed { get; set; }

        public SecurityApiClient Client { get; set; }
    }
}
