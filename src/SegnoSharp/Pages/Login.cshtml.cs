using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Whitestone.SegnoSharp.Pages
{
    public class LoginModel : PageModel
    {
        public async Task OnGet(string redirectUri)
        {
            await HttpContext.ChallengeAsync("oidc", new AuthenticationProperties { RedirectUri = redirectUri });

        }
    }
}
