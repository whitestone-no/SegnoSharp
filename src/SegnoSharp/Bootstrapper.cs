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
using Whitestone.Cambion.Serializer.MessagePack;
using Whitestone.SegnoSharp.Common.Extensions;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Components;
using Whitestone.SegnoSharp.Configuration.Extensions;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.HealthChecks;
using Whitestone.SegnoSharp.Middleware;
using Whitestone.SegnoSharp.Modules;

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
                        .MinimumLevel.Override("Whitestone.SegnoSharp", LogEventLevel.Verbose)
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .WriteTo.File(
                                Path.Combine(builder.Configuration["CommonConfig:DataPath"] ?? string.Empty, "logs", "SegnoSharp.log"),
                                rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true);
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

                    switch (databaseType)
                    {
                        case "sqlite":
                            string connSqlite = hostContext.Configuration.GetConnectionString("SegnoSharpDatabaseSqlite");
                            SqliteConnectionStringBuilder connectionStringBuilder = new(connSqlite);
                            connectionStringBuilder.DataSource = Path.Combine(hostContext.Configuration["CommonConfig:DataPath"] ?? string.Empty, connectionStringBuilder.DataSource);
                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlite(connectionStringBuilder.ConnectionString, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.SQLite")));
                            break;
                        case "mysql":
                            string connMysql = hostContext.Configuration.GetConnectionString("SegnoSharpDatabaseMysql");
                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseMySql(connMysql ?? string.Empty, ServerVersion.AutoDetect(connMysql), x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MySQL")));
                            break;
                        case "postgresql":
                            string connPostgres = hostContext.Configuration.GetConnectionString("SegnoSharpDatabasePostgreSql");
                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseNpgsql(connPostgres ?? string.Empty, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.PostgreSQL")));
                            break;
                        case "mssql":
                            string connMssql = hostContext.Configuration.GetConnectionString("SegnoSharpDatabaseMsSql");
                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlServer(connMssql ?? string.Empty, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MSSQL")));
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
            string databaseType = builder.Configuration.GetSection("Database").GetValue<string>("Type").ToLower();
            var sensitiveDataLogging = builder.Configuration.GetSection("Database").GetValue<bool>("SensitiveDataLogging");

            switch (databaseType)
            {
                case "sqlite":
                    string connSqlite = builder.Configuration.GetConnectionString("SegnoSharpDatabaseSqlite");
                    SqliteConnectionStringBuilder connectionStringBuilder = new(connSqlite);
                    connectionStringBuilder.DataSource = Path.Combine(builder.Configuration["CommonConfig:DataPath"] ?? string.Empty, connectionStringBuilder.DataSource);

                    builder.Services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlite(connectionStringBuilder.ConnectionString, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.SQLite")).EnableSensitiveDataLogging(sensitiveDataLogging));

                    builder.Services.AddHealthChecks().AddSqlite(connectionStringBuilder.ConnectionString, name: "Database");

                    break;
                case "mysql":
                    string connMysql = builder.Configuration.GetConnectionString("SegnoSharpDatabaseMysql");

                    builder.Services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseMySql(connMysql ?? string.Empty, ServerVersion.AutoDetect(connMysql), x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MySQL")).EnableSensitiveDataLogging(sensitiveDataLogging));

                    builder.Services.AddHealthChecks().AddMySql(connMysql ?? string.Empty, name: "Database");

                    break;
                case "postgresql":
                    string connPostgres = builder.Configuration.GetConnectionString("SegnoSharpDatabasePostgreSql");
                    builder.Services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseNpgsql(connPostgres ?? string.Empty, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.PostgreSQL")).EnableSensitiveDataLogging(sensitiveDataLogging));
                    break;
                case "mssql":
                    string connMssql = builder.Configuration.GetConnectionString("SegnoSharpDatabaseMsSql");
                    builder.Services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlServer(connMssql ?? string.Empty, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MSSQL")).EnableSensitiveDataLogging(sensitiveDataLogging));
                    break;
                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}");
            }

            builder.Services.AddHealthChecks().AddDbContextCheck<SegnoSharpDbContext>("DatabaseContext");
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddCambion()
                .UseMessagePackSerializer();

            builder.Services.Configure<CommonConfig>(builder.Configuration.GetSection(CommonConfig.Section));

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

        public static void Configure(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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