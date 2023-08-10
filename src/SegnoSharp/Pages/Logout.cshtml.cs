using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Whitestone.SegnoSharp.Pages
{
    public class LogoutModel : PageModel
    {
        public async void OnGet()
        {
            await HttpContext.SignOutAsync("SegnoSharpAuthCookies");
            await HttpContext.SignOutAsync("oidc", new AuthenticationProperties
            {
                RedirectUri = Url.Content("~/")
            });
        }
    }
}
