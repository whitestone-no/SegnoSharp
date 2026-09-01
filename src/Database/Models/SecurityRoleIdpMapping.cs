using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Whitestone.SegnoSharp.Database.Models;

[Index(nameof(SecurityRoleId), nameof(ClaimValue), IsUnique = true)]
public class SecurityRoleIdpMapping
{
    public int Id { get; set; }
    
    public int SecurityRoleId { get; set; }

    [Required]
    public string ClaimValue { get; set; }   // Role ID from the IdP

    public string Description { get; set; }  // User displayable description of the mapping, i.e. "Moderators"

    public SecurityRole SecurityRole { get; set; }
}