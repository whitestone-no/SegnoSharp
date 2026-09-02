using System.Collections.Generic;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Shared.Permissions;

public sealed class CorePermissions : IPermissionProvider
{
    public const string AlbumsView = "core:albums:view";
    public const string AlbumsViewAll = "core:albums:view:all";
    public const string AlbumsEdit = "core:albums:edit";
    public const string PlaylistView = "core:playlist:view";
    public const string PlaylistEdit = "core:playlist:edit";
    public const string SecurityEdit = "core:security:edit";

    public string PermissionPrefix => "core";

    public IEnumerable<Permission> ProvidedPermissions =>
    [
        new() { Name = AlbumsView, DisplayName = "View albums", Description = "View albums and their content." },
        new() { Name = AlbumsViewAll, DisplayName = "View all albums", Description = "View albums and their content including albums not marked as `IsPublic`." },
        new() { Name = AlbumsEdit, DisplayName = "Edit albums", Description = "Edit albums, their content, and metadata." },
        new() { Name = PlaylistView, DisplayName = "View playlist", Description = "View the playlist." },
        new() { Name = PlaylistEdit, DisplayName = "Edit playlist", Description = "Edit the playlist." },
        new() { Name = SecurityEdit, DisplayName = "Edit security settings", Description = "Edit roles, permissions, and API keys, in security settings.", AllowForApiClients = false}
    ];
}