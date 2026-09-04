using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Configuration.Authentication;
using Whitestone.SegnoSharp.Configuration.Models;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Services;
using Whitestone.SegnoSharp.Shared.Helpers.Security;
using Whitestone.SegnoSharp.Shared.Models.Configuration;
using Whitestone.SegnoSharp.Shared.Models.Security;

namespace Whitestone.SegnoSharp.Configuration.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOidcAuthorizaton(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SegnoSharpOpenIdConnectOptions>(configuration.GetSection(SegnoSharpOpenIdConnectOptions.Section));

            var oidcOptions = configuration.GetSection(SegnoSharpOpenIdConnectOptions.Section).Get<SegnoSharpOpenIdConnectOptions>();

            AuthenticationBuilder authenticationBuilder = services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = AuthenticationSchemes.Cookie;
                    options.DefaultChallengeScheme = AuthenticationSchemes.Oidc;
                })
                .AddCookie(AuthenticationSchemes.Cookie, options =>
                {
                    options.LoginPath = "/auth/login";
                    options.LogoutPath = "/auth/logout";
                    options.AccessDeniedPath = "/access-denied";

                    options.Events.OnValidatePrincipal = ctx =>
                    {
                        if (ctx.Principal is not null && !ctx.Principal.HasClaim(c => c.Type == Constants.AuthenticationSchemeClaim))
                            ctx.Principal.AddIdentity(new ClaimsIdentity(
                                [new Claim(Constants.AuthenticationSchemeClaim, AuthenticationSchemes.Cookie)]));

                        return Task.CompletedTask;
                    };

                    // API paths must get status codes, not redirects to the login page.
                    options.Events.OnRedirectToLogin = ctx => ApiAware(ctx, StatusCodes.Status401Unauthorized);
                    options.Events.OnRedirectToAccessDenied = ctx => ApiAware(ctx, StatusCodes.Status403Forbidden);

                    return;

                    static Task ApiAware<TOptions>(RedirectContext<TOptions> ctx, int statusCode) where TOptions : AuthenticationSchemeOptions
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api"))
                        {
                            ctx.Response.StatusCode = statusCode;
                        }
                        else
                        {
                            ctx.Response.Redirect(ctx.RedirectUri);
                        }

                        return Task.CompletedTask;
                    }
                });

            if (oidcOptions.UseOidc)
            {
                authenticationBuilder.AddOidc(oidcOptions);
            }
            else
            {
                authenticationBuilder
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(AuthenticationSchemes.Oidc, null)
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(AuthenticationSchemes.Bearer, null);
            }

            authenticationBuilder
                .AddScheme<AuthenticationSchemeOptions, ApiKeyHandler>(AuthenticationSchemes.ApiKey, null);

            services.AddSingleton<PermissionRegistry>();

            services.AddSingleton<SecurityRolesSnapshotProvider>();
            services.AddHostedService<SecurityRolesSnapshotRefresher>();
            services.AddSingleton<UnmappedRoleClaimTracker>();

            services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();

            services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddSingleton<ApiKeyGenerator>();
            services.AddSingleton<ApiKeyCache>();
            services.AddSingleton<ApiClientGrantCache>();
            services.AddSingleton<ApiKeyUsageBuffer>();
            services.AddSingleton<ApiKeyFailureTracker>();
            services.AddScoped<ApiKeyStore>();
            services.AddScoped<ApiClientGrantStore>();
            services.AddHostedService<ApiKeyUsageFlusher>();

            services.AddScoped<PermissionChecker>();

            services.AddCascadingAuthenticationState();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.Mcp, policy => policy
                    .AddAuthenticationSchemes(AuthenticationSchemes.Bearer, AuthenticationSchemes.ApiKey)
                    .RequireAuthenticatedUser());
            });

            return services;
        }

        private static void AddOidc(this AuthenticationBuilder builder, SegnoSharpOpenIdConnectOptions oidcOptions)
        {
            builder.AddOpenIdConnect(AuthenticationSchemes.Oidc, options =>
            {
                options.Authority = oidcOptions.Authority;
                options.ClientId = oidcOptions.ClientId;
                options.ClientSecret = oidcOptions.ClientSecret;
                string additionalScopes = oidcOptions.AdditionalScopes;
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

                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

                // Claims from userrinfo endpoint are not automatically mapped into the user,
                // so we need to map them manually
                // `AdminClaimKey` can contain multiple values, so it is mapped to several claims with the same key/type
                options.ClaimActions.MapJsonKey(oidcOptions.RoleClaim, oidcOptions.RoleClaim);
                options.ClaimActions.MapUniqueJsonKey("preferred_username", oidcOptions.UsernameClaimKey);

                // ClaimActions run after token validation, so the name claim type must point at the
                // normalised claim rather than the provider-specific one.
                options.TokenValidationParameters.NameClaimType = "preferred_username";

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
                        if (!oidcOptions.SupportsEndSession)
                        {
                            context.HandleResponse();
                        }

                        return Task.CompletedTask;
                    }
                };
            })
            .AddJwtBearer(AuthenticationSchemes.Bearer, options =>
            {
                options.Authority = oidcOptions.Authority;
                options.Audience = oidcOptions.JwtAudience;

                // Keep "roles" as "roles" so the shared transformation finds it.
                options.MapInboundClaims = false;

                // Internal role names are mirrored to ClaimTypes.Role by the transformation,
                // so the framework's role claim type must match.
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                options.TokenValidationParameters.NameClaimType = oidcOptions.UsernameClaimKey;

                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        // Reject ID tokens presented as access tokens.
                        if (ctx.Principal!.HasClaim(c => c.Type is "nonce" or "at_hash"))
                        {
                            ctx.Fail("ID tokens are not accepted; use an access token.");
                            return Task.CompletedTask;
                        }

                        // Bearer tokens are for users only; machines will use API keys.
                        string sub = ctx.Principal.FindFirst("sub")?.Value;
                        string clientId = ctx.Principal.FindFirst("client_id")?.Value;

                        if (sub is null || string.Equals(sub, clientId, StringComparison.Ordinal))
                        {
                            ctx.Fail("Client credentials tokens are not accepted on this API.");
                        }

                        ctx.Principal!.AddIdentity(new ClaimsIdentity(
                            [new Claim(Constants.AuthenticationSchemeClaim, AuthenticationSchemes.Bearer)]));

                        return Task.CompletedTask;
                    }
                };
            });
        }

        public static void AddCustomDataProtection(this IServiceCollection services, IConfiguration configuration, DirectoryInfo dataFolder)
        {
            configuration.GetSection("DataProtection").GetValue<string>("Folder");
            DirectoryInfo dataProtectionFolder = new(Path.Combine(dataFolder.FullName, configuration.GetSection("DataProtection").GetValue<string>("Folder")));

            if (!dataProtectionFolder.Exists)
            {
                dataProtectionFolder.Create();
            }

            IDataProtectionBuilder builder = services.AddDataProtection()
                .PersistKeysToFileSystem(dataProtectionFolder);

            var certFileName = configuration.GetSection("DataProtection").GetValue<string>("CertificateFile");

            if (string.IsNullOrEmpty(certFileName))
            {
                return;
            }

            FileInfo certFile = new(Path.Combine(dataProtectionFolder.FullName, configuration.GetSection("DataProtection").GetValue<string>("CertificateFile")));
            X509Certificate2 cert = X509CertificateLoader.LoadPkcs12FromFile(certFile.FullName, configuration.GetSection("DataProtection").GetValue<string>("CertificatePassword"), X509KeyStorageFlags.EphemeralKeySet);

            builder.ProtectKeysWithCertificate(cert);
        }
    }
}
