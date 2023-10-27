using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Whitestone.SegnoSharp.BassService.Extensions;
using Serilog;
using Whitestone.Cambion.Extensions;
using Whitestone.Cambion.Serializer.MessagePack;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Configuration.Extensions;
using Whitestone.SegnoSharp.Playlist.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whitestone.SegnoSharp.HealthChecks;
using Whitestone.SegnoSharp.Models.States;

namespace Whitestone.SegnoSharp
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CommonConfig>(_configuration.GetSection(CommonConfig.Section));
            services.Configure<StreamingServer>(_configuration.GetSection(StreamingServer.Section));

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddOidcAuthorizaton(_configuration);
            services.AddCambion()
                .UseMessagePackSerializer();
            services.AddPlaylistHandler(_configuration);
            services.AddBassService(_configuration);

            services.AddScoped<ImportState>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions{ ResponseWriter = HealthCheckResponseWriter.WriteResponse });
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
