using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Whitestone.WASP.Database;

namespace Whitestone.WASP
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
                            .MinimumLevel.Override("Whitestone.WASP", LogEventLevel.Verbose)
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .WriteTo.File(
                                Path.Combine(context.Configuration["CommonConfig:DataPath"], "logs", "wasp.log"),
                                rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true);
                    });

                IHost host = builder.Build();

                using (IServiceScope scope = host.Services.CreateScope())
                {
                    WaspDbContext? dbContext = scope.ServiceProvider.GetService<WaspDbContext>();
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
                    conf.AddEnvironmentVariables("WASP_");
                    conf.AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    string databaseType = context.Configuration.GetSection("Database").GetValue<string>("Type").ToLower();

                    switch (databaseType)
                    {
                        case "sqlite":
                            services.AddDbContext<WaspDbContext, WaspSqliteDbContext>(options =>
                                ConfigureDatabase(options, context.Configuration));
                            break;
                        case "mysql":
                            services.AddDbContext<WaspDbContext, WaspMysqlDbContext>(options =>
                                ConfigureDatabase(options, context.Configuration));
                            break;
                        default:
                            throw new ArgumentException($"Unsupported database type: {databaseType}");
                    }
                });
        }

        public static void ConfigureDatabase(DbContextOptionsBuilder options, IConfiguration configuration)
        {
            string databaseType = configuration.GetSection("Database").GetValue<string>("Type").ToLower();

            switch (databaseType)
            {
                case "sqlite":
                    string connSqlite = configuration.GetConnectionString("WaspDatabaseSqlite");
                    SqliteConnectionStringBuilder connectionStringBuilder = new(connSqlite);
                    connectionStringBuilder.DataSource = Path.Combine(configuration["CommonConfig:DataPath"], connectionStringBuilder.DataSource);
                    options.UseSqlite(connectionStringBuilder.ConnectionString);
                    break;
                case "mysql":
                    string connMysql = configuration.GetConnectionString("WaspDatabaseMysql");
                    options.UseMySql(connMysql, ServerVersion.AutoDetect(connMysql));
                    break;
                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}");
            }
        }
    }
}