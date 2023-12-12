using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Common.Models.Configuration;

namespace Whitestone.SegnoSharp.Shared
{
    public partial class MainLayout
    {
        [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
        [Inject] private IOptions<CommonConfig> CommonConfig { get; set; }

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
