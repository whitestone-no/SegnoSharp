using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;

namespace Whitestone.WASP.Configuration.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOidcAuthorizaton(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "WaspAuthCookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("WaspAuthCookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = configuration.GetSection("OpenIdConnect").GetValue<string>("Authority");
                options.ClientId = configuration.GetSection("OpenIdConnect").GetValue<string>("ClientId");
                options.ClientSecret = configuration.GetSection("OpenIdConnect").GetValue<string>("ClientSecret");
                options.Scope.Add("roles");
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ClaimActions.MapUniqueJsonKey("role", "role");
                options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");

                options.Events = new OpenIdConnectEvents
                {
                    OnAccessDenied = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/");
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    // Would love to use .RequireRole() here, but somehow the "role" claims from IDP is not mapped to user roles.
                    .RequireAssertion(ctx =>
                    {
                        Claim? claim = ctx.User.FindFirst("role");
                        return claim != null && claim.Value.Contains(configuration.GetSection("OpenIdConnect").GetValue<string>("AdminRoleId"));
                    })
                    .Build();
                options.AddPolicy("IgnoreRole",
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build());
            });

            return services;
        }
    }
}
