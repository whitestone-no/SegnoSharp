using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Whitestone.SegnoSharp.Configuration.Authentication
{
    public class FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder), IAuthenticationSignOutHandler
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Fake User"),
                new Claim(ClaimTypes.NameIdentifier, "fake-user"),
                new Claim("preferred_username", "\u26a0\ufe0f FAKE USER - FOR LOCAL USE ONLY! \u26a0\ufe0f"),
                new Claim(
                    configuration.GetSection("OpenIdConnect").GetValue<string>("RoleClaim"),
                    configuration.GetSection("OpenIdConnect").GetValue<string>("AdminRole"))
            };

            var identity = new ClaimsIdentity(claims, "oidc");
            var principal = new ClaimsPrincipal(identity);

            await Context.SignInAsync("SegnoSharpAuthCookies", principal);

            Context.Response.Redirect(properties.RedirectUri!);
        }

        public async Task SignOutAsync(AuthenticationProperties properties)
        {
            await Context.SignOutAsync("SegnoSharpAuthCookies");

            Context.Response.Redirect(properties!.RedirectUri!);
        }
    }
}
