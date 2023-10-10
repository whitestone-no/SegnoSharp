using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Whitestone.SegnoSharp.Database;

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
                IHostBuilder builder = CreateHostBuilder(args);

                builder.ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    })
                    .UseSerilog((context, services, configuration) =>
                    {
                        configuration
                            .ReadFrom.Services(services)
                            .MinimumLevel.Override("Whitestone.SegnoSharp", LogEventLevel.Verbose)
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .WriteTo.File(
                                Path.Combine(context.Configuration["CommonConfig:DataPath"], "logs", "SegnoSharp.log"),
                                rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true);
                    });

                IHost host = builder.Build();

                using (IServiceScope scope = host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetService<SegnoSharpDbContext>();
                    await dbContext?.Database?.EnsureCreatedAsync();
                    await dbContext?.Database.MigrateAsync()!;
                }

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        // This method is run instead of Main() when doing EF migrations. Keep it as simple as possible (database settings only)
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(conf =>
                {
                    conf.AddJsonFile("appsettings.json");
                    conf.AddUserSecrets("ef2b06ee-7634-4a6e-9cce-5ad721a03d65");
                    conf.AddEnvironmentVariables("SegnoSharp_");
                    conf.AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    string databaseType = context.Configuration.GetSection("Database").GetValue<string>("Type").ToLower();

                    switch (databaseType)
                    {
                        case "sqlite":
                            string connSqlite = context.Configuration.GetConnectionString("SegnoSharpDatabaseSqlite");
                            SqliteConnectionStringBuilder connectionStringBuilder = new(connSqlite);
                            connectionStringBuilder.DataSource = Path.Combine(context.Configuration["CommonConfig:DataPath"] ?? string.Empty, connectionStringBuilder.DataSource);

                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseSqlite(connectionStringBuilder.ConnectionString, x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.SQLite")));

                            services.AddHealthChecks().AddSqlite(connectionStringBuilder.ConnectionString, name: "Database");

                            break;
                        case "mysql":
                            string connMysql = context.Configuration.GetConnectionString("SegnoSharpDatabaseMysql");

                            services.AddDbContextFactory<SegnoSharpDbContext>(options => options.UseMySql(connMysql ?? string.Empty, ServerVersion.AutoDetect(connMysql), x => x.MigrationsAssembly("Whitestone.SegnoSharp.Database.Migrations.MySQL")));

                            services.AddHealthChecks().AddMySql(connMysql ?? string.Empty, name: "Database");

                            break;
                        default:
                            throw new ArgumentException($"Unsupported database type: {databaseType}");
                    }

                    services.AddHealthChecks().AddDbContextCheck<SegnoSharpDbContext>("DatabaseContext");
                });
        }
    }
}