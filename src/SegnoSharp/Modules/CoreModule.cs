using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Security;
using Whitestone.SegnoSharp.Shared.Permissions;

namespace Whitestone.SegnoSharp.Modules;

public class CoreModule : IModule, IPermissionProvider
{
    public Guid Id => new("c661e55b-8dbf-4ca1-b35b-8f4345b4c983");

    public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
        
    }

    public string PermissionPrefix => "core";

    public IEnumerable<Permission> ProvidedPermissions =>
    [
        new() { Name = CorePermissions.AlbumsView, DisplayName = "View albums", Description = "View albums and their content." },
        new() { Name = CorePermissions.AlbumsViewAll, DisplayName = "View all albums", Description = "View albums and their content including albums not marked as `IsPublic`." },
        new() { Name = CorePermissions.AlbumsEdit, DisplayName = "Edit albums", Description = "Edit albums, their content, and metadata." },
        new() { Name = CorePermissions.PlaylistView, DisplayName = "View playlist", Description = "View the playlist." },
        new() { Name = CorePermissions.PlaylistEdit, DisplayName = "Edit playlist", Description = "Edit the playlist." },
        new() { Name = CorePermissions.SecurityEdit, DisplayName = "Edit security settings", Description = "Edit roles, permissions, and API keys, in security settings.", AllowForApiClients = false}
    ];
}