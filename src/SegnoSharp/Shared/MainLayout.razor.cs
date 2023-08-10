using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Whitestone.SegnoSharp.Shared
{
    public partial class MainLayout
    {
        [Inject]
        private AuthenticationStateProvider AuthState { get; set; } = null!;

        private string _loggedInAs = null!;

        protected override async Task OnInitializedAsync()
        {
            AuthenticationState state = await AuthState.GetAuthenticationStateAsync();

            _loggedInAs = state.User.Claims
                .Where(c => c.Type.Equals("preferred_username"))
                .Select(c => c.Value)
                .FirstOrDefault() ?? "[Unknown username]";

            await base.OnInitializedAsync();
        }
    }
}
