using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Whitestone.WASP.BassService.Extensions;
using Serilog;
using Whitestone.Cambion.Extensions;
using Whitestone.Cambion.Serializer.MessagePack;
using Whitestone.WASP.Common.Models.Configuration;
using Whitestone.WASP.Configuration.Extensions;
using Whitestone.WASP.Database;
using Whitestone.WASP.Playlist.Extensions;

namespace Whitestone.WASP
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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
