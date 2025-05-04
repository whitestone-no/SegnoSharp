using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Configuration.Authentication;
using Whitestone.SegnoSharp.Shared.Models.Configuration;

namespace Whitestone.SegnoSharp.Configuration.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOidcAuthorizaton(this IServiceCollection services, IConfiguration configuration)
        {
            AuthenticationBuilder authenticationBuilder = services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "SegnoSharpAuthCookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("SegnoSharpAuthCookies");

            if (configuration.GetSection("OpenIdConnect").GetValue<bool>("UseOidc"))
            {
                authenticationBuilder.AddOidc(configuration);
            }
            else
            {
                authenticationBuilder.AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("oidc", null);
            }

            services.AddAuthorization(options =>
            {
                var adminClaimKey = configuration.GetSection("OpenIdConnect").GetValue<string>("AdminClaimKey");
                var adminClaimValue = configuration.GetSection("OpenIdConnect").GetValue<string>("AdminClaimValue");

                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    // Would love to use .RequireRole() here, but somehow the "role" claims from IDP is not mapped to user roles.
                    .RequireAssertion(ctx =>
                    {
                        Claim claim = ctx.User.FindFirst(adminClaimKey);
                        return claim != null && claim.Value.Contains(adminClaimValue);
                    })
                    .Build();
                options.AddPolicy("IgnoreRole",
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build());
            });

            return services;
        }

        private static void AddOidc(this AuthenticationBuilder builder, IConfiguration configuration)
        {
            builder.AddOpenIdConnect("oidc", options =>
            {
                options.Authority = configuration.GetSection("OpenIdConnect").GetValue<string>("Authority");
                options.ClientId = configuration.GetSection("OpenIdConnect").GetValue<string>("ClientId");
                options.ClientSecret = configuration.GetSection("OpenIdConnect").GetValue<string>("ClientSecret");
                var additionalScopes =
                    configuration.GetSection("OpenIdConnect").GetValue<string>("AdditionalScopes");
                if (!string.IsNullOrEmpty(additionalScopes))
                {
                    foreach (string scope in additionalScopes.Split(","))
                    {
                        options.Scope.Add(scope);
                    }
                }
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ClaimActions.MapUniqueJsonKey("preferred_username",
                    configuration.GetSection("OpenIdConnect").GetValue<string>("UsernameClaimKey"));
                // If the admin claim key is part of the access token, then this is not necessary
                // but if it is part of the userinfo endpoint then it must be mapped into the regular claims
                // This ensures that "AdminClaimKey" will always be available whether it comes from access token or user info
                options.ClaimActions.MapUniqueJsonKey(
                    configuration.GetSection("OpenIdConnect").GetValue<string>("AdminClaimKey"),
                    configuration.GetSection("OpenIdConnect").GetValue<string>("AdminClaimKey"));

                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

                options.Events = new OpenIdConnectEvents
                {
                    OnAccessDenied = context =>
                    {
                        var siteConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<SiteConfig>>();

                        context.HandleResponse();
                        context.Response.Redirect(siteConfig.Value.BasePath);

                        return Task.CompletedTask;
                    },
                    OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        if (!configuration.GetSection("OpenIdConnect").GetValue<bool>("SupportsEndSession"))
                        {
                            context.HandleResponse();
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}
