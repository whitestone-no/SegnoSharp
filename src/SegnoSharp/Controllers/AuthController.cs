using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Whitestone.SegnoSharp.Controllers
{
    [ApiController]
    [Route("/[controller]/[action]")]
    public class AuthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Login([FromQuery]string redirectUri)
        {
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, "oidc");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            return SignOut(new AuthenticationProperties { RedirectUri = Url.Content("~/") }, "oidc", "SegnoSharpAuthCookies");
        }
    }
}
