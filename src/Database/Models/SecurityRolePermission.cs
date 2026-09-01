using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Whitestone.SegnoSharp.Database.Models;

[Index(nameof(SecurityRoleId), nameof(Permission), IsUnique = true)]
public class SecurityRolePermission
{
    public int Id { get; set; }
    public int SecurityRoleId { get; set; }
    public string Permission { get; set; }

    public SecurityRole SecurityRole { get; set; }
}