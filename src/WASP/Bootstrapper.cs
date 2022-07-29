using Serilog;
using Serilog.Events;

namespace Whitestone.WASP
{
    public class Bootstrapper
    {
        public static async Task Main(string[] args)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                IHostBuilder builder = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(conf =>
                    {
                        conf.AddJsonFile("appsettings.json");
                        conf.AddUserSecrets("ef2b06ee-7634-4a6e-9cce-5ad721a03d65");
                        conf.AddEnvironmentVariables("WASP_");
                    })
                    .ConfigureWebHostDefaults(webBuilder =>
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
                            .WriteTo.File(Path.Combine(context.Configuration["DataPath"], "logs", "wasp.log"),
                                rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true);
                    });

                IHost host = builder.Build();

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

        //private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        //{
        //    string? executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    string libraryPath = Path.Combine(executingFolder ?? Directory.GetCurrentDirectory(), "lib", "Bass.Net");

        //    if (args.Name.StartsWith("Bass.Net"))
        //    {
        //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //        {
        //            return Assembly.LoadFrom(Path.Combine(libraryPath, "Bass.Net.Linux.dll"));
        //        }
        //        else
        //        {
        //            return Assembly.LoadFrom(Path.Combine(libraryPath, "Bass.Net.dll"));
        //        }
        //    }
            
        //    return Assembly.LoadFrom("");
        //}
    }
}