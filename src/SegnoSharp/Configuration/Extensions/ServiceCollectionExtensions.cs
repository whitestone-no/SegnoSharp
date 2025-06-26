using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
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
            services.Configure<SegnoSharpOpenIdConnectOptions>(configuration.GetSection(SegnoSharpOpenIdConnectOptions.Section));

            var oidcOptions = configuration.GetSection(SegnoSharpOpenIdConnectOptions.Section).Get<SegnoSharpOpenIdConnectOptions>();

            AuthenticationBuilder authenticationBuilder = services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "SegnoSharpAuthCookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("SegnoSharpAuthCookies");

            if (oidcOptions.UseOidc)
            {
                authenticationBuilder.AddOidc(oidcOptions);
            }
            else
            {
                authenticationBuilder.AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("oidc", null);
            }

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireRole(oidcOptions.AdminRole)
                    .Build();
                options.AddPolicy("IgnoreRole",
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build());
            });

            services.AddTransient<IClaimsTransformation, RoleClaimsTransformation>();

            return services;
        }

        private static void AddOidc(this AuthenticationBuilder builder, SegnoSharpOpenIdConnectOptions oidcOptions)
        {
            builder.AddOpenIdConnect("oidc", options =>
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
