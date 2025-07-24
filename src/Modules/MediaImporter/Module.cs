using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models.Config;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models.Persistent;

namespace Whitestone.SegnoSharp.Modules.MediaImporter
{
    public class Module : IModule
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
            services.Configure<MediaImporterConfig>(configuration.GetSection(MediaImporterConfig.Section));

            services.AddScoped<ImportState>();

            services.AddSingleton(sp =>
            {
                MediaImporterSettings settings = new();
                var pm = sp.GetRequiredService<IPersistenceManager>();
                pm.Register(settings);
                return settings;
            });
        }
    }
}
