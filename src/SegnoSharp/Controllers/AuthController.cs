using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Shared.Models.Configuration;

namespace Whitestone.SegnoSharp.Controllers
{
    [ApiController]
    [Route("/[controller]/[action]")]
    public class AuthController(IOptions<SiteConfig> siteConfig) : ControllerBase
    {
        [HttpGet]
        public IActionResult Login([FromQuery]string redirectUri)
        {
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, "oidc");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            return SignOut(new AuthenticationProperties { RedirectUri = Url.Content(siteConfig.Value.BasePath) }, "oidc", "SegnoSharpAuthCookies");
        }
    }
}
