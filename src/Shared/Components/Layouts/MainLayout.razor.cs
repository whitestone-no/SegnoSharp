using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Shared.Attributes;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Configuration;
using Whitestone.SegnoSharp.Shared.ViewModels;

namespace Whitestone.SegnoSharp.Shared.Components.Layouts
{
    public partial class MainLayout
    {
        [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
        [Inject] private IOptions<SiteConfig> SiteConfig { get; set; }
        [Inject] private IEnumerable<IModule> Modules { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        private string _loggedInAs = null!;
        private List<MenuNavigationModel> ModuleNavItems { get; set; } = [];
        private bool ShowHome { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrEmpty(SiteConfig.Value.StartPage))
            {
                ShowHome = false;
            }

            AuthenticationState state = await AuthState.GetAuthenticationStateAsync();

            _loggedInAs = state.User.Claims
                .Where(c => c.Type.Equals("preferred_username"))
                .Select(c => c.Value)
                .FirstOrDefault() ?? "[Unknown username]";

            foreach (IModule module in Modules)
            {
                foreach (Type moduleType in module.GetType().Assembly.GetTypes())
                {
                    if (!moduleType.IsAssignableTo(typeof(IComponent)))
                    {
                        continue;
                    }

                    if (moduleType.GetCustomAttribute<RouteAttribute>() is not { } route ||
                        moduleType.GetCustomAttribute<ModuleMenuAttribute>() is not { Parent: null } moduleMenu)
                    {
                        continue;
                    }

                    // This is a legitimate @page

                    MenuNavigationModel nav = new()
                    {
                        Id = module.Id,
                        MenuTitle = moduleMenu.MenuTitle,
                        Path = route.Template.TrimStart('/'),
                        Icon = moduleMenu.Icon ?? "fa-file",
                        IsAdmin = moduleMenu.IsAdmin,
                        SortOrder = moduleMenu.SortOrder
                    };

                    // Find all menu childs
                    
                    foreach (Type childModuleType in module.GetType().Assembly.GetTypes())
                    {
                        if (!childModuleType.IsAssignableTo(typeof(IComponent)))
                        {
                            continue;
                        }

                        if (childModuleType.GetCustomAttribute<RouteAttribute>() is not { } childRoute ||
                            childModuleType.GetCustomAttribute<ModuleMenuAttribute>() is not { } childModuleMenu ||
                            childModuleMenu.Parent != moduleType)
                        {
                            continue;
                        }

                        BaseMenuNavigation childNav = new()
                        {
                            MenuTitle = childModuleMenu.MenuTitle,
                            Path = childRoute.Template.TrimStart('/'),
                            SortOrder = childModuleMenu.SortOrder
                        };

                        nav.Children.Add(childNav);
                    }

                    ModuleNavItems.Add(nav);
                }
            }

            await base.OnInitializedAsync();
        }

        private bool IsExpanded(string path)
        {
            return NavigationManager.ToBaseRelativePath(NavigationManager.Uri).StartsWith(path) &&
                   !NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Equals(path);
        }

    }
}
