using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Shared.Attributes.Controllers;
using Whitestone.SegnoSharp.Shared.Models.Configuration;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Controllers
{
    [ApiController]
    [SkipGlobalRoutePrefix]
    [Route("/[controller]/[action]")]
    public class AuthController(IOptions<SiteConfig> siteConfig) : ControllerBase
    {
        [HttpGet]
        public IActionResult Login([FromQuery]string redirectUri)
        {
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, AuthenticationSchemes.Oidc);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            return SignOut(new AuthenticationProperties { RedirectUri = Url.Content(siteConfig.Value.BasePath) }, AuthenticationSchemes.Oidc, AuthenticationSchemes.Cookie);
        }
    }
}
