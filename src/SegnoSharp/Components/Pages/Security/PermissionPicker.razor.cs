using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Modules;
using Whitestone.SegnoSharp.Services;

namespace Whitestone.SegnoSharp.Components.Pages.Security;

public partial class PermissionPicker
{
    [Inject]
    private PermissionRegistry Registry { get; set; }

    /// <summary>Granted permission names. Mutated in place by this component.</summary>
    [Parameter, EditorRequired]
    public HashSet<string> Selected { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Raised after a change so the parent can track unsaved edits.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    /// <summary>Hide the picker and show a note instead; used for the wildcard system role.</summary>
    [Parameter]
    public bool Wildcard { get; set; }

    /// <summary>Restrict the list to permissions that may be granted to API clients.</summary>
    [Parameter]
    public bool ApiClientsOnly { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private sealed record PermissionGroup(string Label, List<RegisteredPermission> Permissions);

    private List<PermissionGroup> _groups = [];
    private List<string> _orphaned = [];

    protected override void OnParametersSet()
    {
        _groups = Registry.ByPlugin()
            .OrderBy(g => g.Key == typeof(CoreModule).FullName ? 0 : 1)
            .ThenBy(g => g.Key)
            .Select(group => new PermissionGroup(
                Label: group.Key == typeof(CoreModule).FullName ? "Core" : group.Key,
                Permissions: group
                    .Where(permission => !ApiClientsOnly || permission.Permission.AllowForApiClients)
                    .ToList()))
            .Where(group => group.Permissions.Count > 0)
            .ToList();

        RecalculateOrphans();
    }

    private void RecalculateOrphans() =>
        _orphaned = Selected
            .Where(name => name != PermissionRegistry.Wildcard && !Registry.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

    private async Task ToggleAsync(string name, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            Selected.Add(name);
        }
        else
        {
            Selected.Remove(name);
        }

        await OnChanged.InvokeAsync();
    }

    private async Task RemoveAsync(string name)
    {
        Selected.Remove(name);
        RecalculateOrphans();

        await OnChanged.InvokeAsync();
    }
}