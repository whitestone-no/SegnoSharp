using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Whitestone.Cambion.Extensions;
using Whitestone.SegnoSharp.Shared.Extensions;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Configuration;
using Whitestone.SegnoSharp.Components;
using Whitestone.SegnoSharp.Configuration.Extensions;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.HealthChecks;
using Whitestone.SegnoSharp.Middleware;
using Whitestone.SegnoSharp.Modules;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Whitestone.SegnoSharp
{
    public class Bootstrapper

    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            
            try
            {
                WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

                builder.Configuration.AddJsonFile("appsettings.json");
                builder.Configuration.AddUserSecrets(typeof(Bootstrapper).Assembly);
                builder.Configuration.AddEnvironmentVariables("SegnoSharp_");

                builder.Services.AddSerilog((services, config) =>
                {
                    config
                        .ReadFrom.Services(services)
                        .ReadFrom.Configuration(builder.Configuration);
                });

                builder.ConfigureServices();

                // Add these again as modules may have overwritten some values
                // The priority is:
                // CommandLine arguments > Environment variables > module secrets > module appsettings > SegnoSharp secrets > SegnoSharp appsettings
                builder.Configuration.AddEnvironmentVariables("SegnoSharp_");
                builder.Configuration.AddCommandLine(args);

                WebApplication app = builder.Build();
                app.Configure();

                using (IServiceScope scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetService<SegnoSharpDbContext>();
                    await dbContext?.Database.MigrateAsync()!;
                }

                await app.RunAsync();
            }
            catch (Exception ex)
                when (ex is not HostAbortedException &&
                      ex.Source != "Microsoft.EntityFrameworkCore.Design") // see https://github.com/dotnet/efcore/issues/29923
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        // ReSharper disable once UnusedMember.Global
        // This is used by Entity Framework when running migrations
        // Keep as minimal as possible
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile("appsettings.json");
                    c.AddUserSecrets(typeof(Bootstrapper).Assembly);
                    c.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    string databaseType = hostContext.Configuration.GetSection("Database").GetValue<string>("Type").ToLower();
                    string connectionString = hostContext.Configuration.GetConnectionString("SegnoSharp");

                    switch (databaseType)
                    {
                        case "sqlite":
                            SqliteConnectionStringBuilder connectionStringBuilder = new(connectionString);
                            connectionStringBuilder.DataSource = Path.Combine(hostContext.Configuration["SiteConfig:DataPath"] ?? string.Empty, connectionStringBuilder.DataSource);
                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlite(connectionStringBuilder.ConnectionString, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.SQLite")));
                            break;
                        case "mysql":
                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseMySql(connectionString ?? string.Empty, ServerVersion.AutoDetect(connectionString), x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MySQL")));
                            break;
                        case "postgresql":
                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseNpgsql(connectionString ?? string.Empty, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.PostgreSQL")));
                            break;
                        case "mssql":
                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlServer(connectionString ?? string.Empty, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MSSQL")));
                            break;
                        default:
                            throw new ArgumentException($"Unsupported database type: {databaseType}");
                    }
                });
        }
    }

    public static class StartupExtensions
    {
        public static void ConfigureServices(this WebApplicationBuilder builder)
        {
            DirectoryInfo dataFolder = builder.GetDataFolder();
            if (dataFolder == null)
            {
                Log.Fatal("Could not find data folder. Either SiteConfig:DataPath is not set, or the folder doesn't exist");
                return;
            }

            string databaseType = builder.Configuration.GetSection("Database").GetValue<string>("Type").ToLower();
            string connectionString = builder.Configuration.GetConnectionString("SegnoSharp");
            var sensitiveDataLogging = builder.Configuration.GetSection("Database").GetValue<bool>("SensitiveDataLogging");

            switch (databaseType)
            {
                case "sqlite":
                    SqliteConnectionStringBuilder connectionStringBuilder = new(connectionString);
                    connectionStringBuilder.DataSource = Path.Combine(builder.Configuration["SiteConfig:DataPath"] ?? string.Empty, connectionStringBuilder.DataSource);

                    builder.Services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlite(connectionStringBuilder.ConnectionString, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.SQLite")).EnableSensitiveDataLogging(sensitiveDataLogging));
                    builder.Services.AddHealthChecks().AddSqlite(connectionStringBuilder.ConnectionString, name: "Database");

                    break;
                case "mysql":
                    builder.Services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseMySql(connectionString ?? string.Empty, ServerVersion.AutoDetect(connectionString), x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MySQL")).EnableSensitiveDataLogging(sensitiveDataLogging));
                    builder.Services.AddHealthChecks().AddMySql(connectionString ?? string.Empty, name: "Database");
                    break;
                case "postgresql":
                    builder.Services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseNpgsql(connectionString ?? string.Empty, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.PostgreSQL")).EnableSensitiveDataLogging(sensitiveDataLogging));
                    builder.Services.AddHealthChecks().AddNpgSql(connectionString ?? string.Empty, name: "Database");
                    break;
                case "mssql":
                    builder.Services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlServer(connectionString ?? string.Empty, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MSSQL")).EnableSensitiveDataLogging(sensitiveDataLogging));
                    builder.Services.AddHealthChecks().AddSqlServer(connectionString ?? string.Empty, name: "Database");
                    break;
                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}");
            }

            builder.Services.AddHealthChecks().AddDbContextCheck<SegnoSharpDbContext>("DatabaseContext");
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddCambion();

            builder.Services.Configure<SiteConfig>(builder.Configuration.GetSection(SiteConfig.Section));

            IEnumerable<IModule> modules = builder.AddModules();

            IMvcBuilder controllerBuilder = builder.Services.AddControllers();

            foreach (IModule module in modules)
            {
                controllerBuilder.AddApplicationPart(module.GetType().Assembly);
            }

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddOidcAuthorizaton(builder.Configuration);
            builder.Services.AddCommon();
        }

        private static DirectoryInfo GetDataFolder(this WebApplicationBuilder builder)
        {
            string dataFolderPath = builder.Configuration["SiteConfig:DataPath"];
            if (dataFolderPath == null)
            {
                return null;
            }

            DirectoryInfo dataFolder = new(dataFolderPath);

            return dataFolder.Exists ? dataFolder : null;
        }

        public static void Configure(this WebApplication app)
        {
            var siteConfig = app.Services.GetRequiredService<IOptions<SiteConfig>>();

            app.UsePathBase(siteConfig.Value.BasePath);

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (siteConfig.Value.BehindProxy)
            {
                ForwardedHeadersOptions options = new()
                {
                    ForwardedHeaders =
                        ForwardedHeaders.XForwardedFor |
                        ForwardedHeaders.XForwardedProto |
                        ForwardedHeaders.XForwardedHost
                };

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();

                app.UseForwardedHeaders(options);
            }

            app.UseSerilogRequestLogging();

            app.UseStaticFiles();

            app.UseModuleEmbeddedResource();

            app.UseRouting();

            app.UseAntiforgery();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter.WriteResponse });
            app.MapControllers();

            Assembly[] moduleAssemblies = app.Services.GetServices<IModule>()
                .Select(p => p.GetType().Assembly)
                .Distinct()
                .ToArray();

            app.MapRazorComponents<App>()
                .AddAdditionalAssemblies(moduleAssemblies)
                .AddInteractiveServerRenderMode();
        }
    }
}