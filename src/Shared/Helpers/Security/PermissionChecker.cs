using Microsoft.AspNetCore.Components.Authorization;
using System.Threading.Tasks;

namespace Whitestone.SegnoSharp.Shared.Helpers.Security;

/// <summary>
/// Checks permissions for the signed-in user. Inject this into a component and call it from
/// any handler that performs a privileged action: AuthorizeView only hides the control, it
/// does not prevent the handler from being invoked over the circuit.
/// </summary>
/// <remarks>
/// Blazor only, since it resolves the user through the circuit. API controllers
/// can use PermissionAuthorizer directly.
/// </remarks>
public sealed class PermissionChecker(
    PermissionAuthorizer authorizer,
    AuthenticationStateProvider authenticationStateProvider)
{
    /// <summary>True when the user holds any one of the permissions.</summary>
    public Task<bool> HasAnyAsync(params string[] permissions)
    {
        return EvaluateAsync(permissions, false);
    }

    /// <summary>True when the user holds all of the permissions.</summary>
    public Task<bool> HasAllAsync(params string[] permissions)
    {
        return EvaluateAsync(permissions, true);
    }

    private async Task<bool> EvaluateAsync(string[] permissions, bool all)
    {
        // Deliberately not cached: permissions can change while a circuit is alive, and a
        // stale "yes" is the failure that matters.
        AuthenticationState state = await authenticationStateProvider.GetAuthenticationStateAsync();

        return all
            ? await authorizer.HasAllAsync(state.User, permissions)
            : await authorizer.HasAnyAsync(state.User, permissions);
    }
}