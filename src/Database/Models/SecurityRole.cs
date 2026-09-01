using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Whitestone.SegnoSharp.Database.Models;

public class SecurityRole
{
    [NotMapped]
    public const int AdministratorRoleId = 1;

    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public string Description { get; set; }
    public bool IsSystem { get; set; }

    public List<SecurityRoleIdpMapping> IdpMappings { get; set; } = [];
    public List<SecurityRolePermission> Permissions { get; set; } = [];
}