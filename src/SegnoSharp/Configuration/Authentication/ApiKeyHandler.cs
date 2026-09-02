using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Services;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Configuration.Authentication;

public sealed class ApiKeyHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    ApiKeyStore store,
    ApiKeyFailureTracker failures)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string HeaderName = "X-Api-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out StringValues raw) ||
            string.IsNullOrEmpty(raw))
        {
            // This will cause the following debug message to be logged:
            // {AuthenticationSchemes.ApiKey} was not authenticated.
            // This is expected behaviour and will be logged for every request that does not include an API key.
            return AuthenticateResult.NoResult();
        }

        if (failures.IsBlocked(Context.Connection.RemoteIpAddress))
        {
            return AuthenticateResult.Fail("Too many failed attempts.");
        }

        ApiKeyValidationResult result = await store.ValidateAsync(raw.ToString()!, Context.RequestAborted);

        if (result.Outcome != ApiKeyValidationOutcome.Success)
        {
            failures.RecordFailure(Context.Connection.RemoteIpAddress);

            // A known prefix with a bad secret is usually a real client with a stale
            // key — worth distinguishing internally, never in the response.
            Logger.LogInformation("API key rejected: {Outcome} (prefix {Prefix}).", result.Outcome, result.Prefix);

            return AuthenticateResult.Fail("Invalid API key.");
        }

        ClaimsIdentity identity = new(
        [
            new Claim(Constants.AuthenticationSchemeClaim, AuthenticationSchemes.ApiKey),
            new Claim(Constants.ClientIdClaim, result.ApiClientId.ToString()),
            new Claim(Constants.ClientNameClaim, result.DisplayName),
            new Claim(Constants.KeyPrefixClaim, result.Prefix)
        ],
        Scheme.Name,
        Constants.ClientNameClaim,
        ClaimTypes.Role);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = HeaderName;

        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        
        return Task.CompletedTask;
    }
}