using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        [Inject] private IAuthorizationService AuthorizationService { get; set; }
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] private ILogger<MainLayout> Logger { get; set; }

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
            ClaimsPrincipal user = state.User;

            _loggedInAs = user.FindFirst("preferred_username")?.Value ?? "[Unknown username]";

            Dictionary<Type, bool> visibility = [];

            foreach (IModule module in Modules)
            {
                Type[] moduleTypes;
                try
                {
                    moduleTypes = module.GetType().Assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Types is Type?[]; a null entry has no name, so the loader exceptions are the only
                    // source of detail about what could not be loaded.
                    string[] reasons = ex.LoaderExceptions
                        .Where(loaderException => loaderException is not null)
                        .Select(loaderException => loaderException!.Message)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();

                    Logger.LogWarning(ex,
                        "{Failed} of {Total} types in module {ModuleName} could not be loaded, so their menu entries and declared permissions are missing. Reasons: {Reasons}",
                        ex.Types.Count(type => type is null),
                        ex.Types.Length,
                        module.GetType().FullName,
                        reasons.Length > 0 ? string.Join(" | ", reasons) : "none reported");

                    moduleTypes = ex.Types.Where(type => type is not null).ToArray()!;
                }

                foreach (Type moduleType in moduleTypes)
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

                    // This is a legitimate @page at this point

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
                    
                    foreach (Type childModuleType in moduleTypes)
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

                        if (!await VisibleAsync(childModuleType))
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

                    if (!await VisibleAsync(moduleType))
                    {
                        // Hide the branch entirely when nothing under it is reachable either.
                        if (nav.Children.Count == 0)
                        {
                            continue;
                        }

                        // Otherwise keep the group but point it at a child the user can actually open,
                        // so the parent link never leads to an access denied page.
                        nav.Path = nav.Children.OrderBy(child => child.SortOrder).First().Path;
                    }

                    ModuleNavItems.Add(nav);
                }
            }

            await base.OnInitializedAsync();

            return;

            async Task<bool> VisibleAsync(Type type)
            {
                if (visibility.TryGetValue(type, out bool cached))
                {
                    return cached;
                }

                bool visible = await IsVisibleAsync(user, type);
                visibility[type] = visible;
                return visible;
            }
        }

        private bool IsExpanded(string path)
        {
            return NavigationManager.ToBaseRelativePath(NavigationManager.Uri).StartsWith(path) &&
                   !NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Equals(path);
        }

        /// <summary>
        /// Whether a routable component should appear in the menu. Evaluates the same policies the
        /// authorization middleware would, so the menu cannot show a page the user would be denied.
        /// </summary>
        private async Task<bool> IsVisibleAsync(ClaimsPrincipal user, Type componentType)
        {
            // AllowAnonymous overrides any [Authorize] on the same type.
            if (componentType.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            {
                return true;
            }

            // inherit: true so a module base component carrying [Authorize] is honoured.
            foreach (AuthorizeAttribute attribute in componentType.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
            {
                if (!await SatisfiesAsync(user, attribute))
                {
                    return false;
                }
            }

            // No attributes: a public page.
            return true;
        }

        private async Task<bool> SatisfiesAsync(ClaimsPrincipal user, AuthorizeAttribute attribute)
        {
            bool hasPolicy = !string.IsNullOrEmpty(attribute.Policy);
            bool hasRoles = !string.IsNullOrEmpty(attribute.Roles);

            // A bare [Authorize] requires only an authenticated user.
            if (!hasPolicy && !hasRoles)
            {
                return user.Identity?.IsAuthenticated == true;
            }

            if (hasPolicy)
            {
                AuthorizationResult result = await AuthorizationService.AuthorizeAsync(user, attribute.Policy!);

                if (!result.Succeeded)
                {
                    return false;
                }
            }

            if (!hasRoles)
            {
                return true;
            }

            bool inAnyRole = attribute.Roles
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(user.IsInRole);

            return inAnyRole;
        }
    }
}
