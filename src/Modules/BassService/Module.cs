using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Modules.BassService.Helpers;
using Whitestone.SegnoSharp.Modules.BassService.Interfaces;
using Whitestone.SegnoSharp.Modules.BassService.Models.Config;

namespace Whitestone.SegnoSharp.Modules.BassService
{
    public class Module : IModule
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {

            services.Configure<BassRegistration>(configuration.GetSection(BassRegistration.Section));
            services.Configure<Ffmpeg>(configuration.GetSection(Ffmpeg.Section));

            services.AddSingleton<IBassWrapper, BassWrapper>();
            
            services.AddHostedService<BassServiceHost>();
        }
    }
}
