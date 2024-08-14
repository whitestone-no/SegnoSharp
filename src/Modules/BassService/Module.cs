using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Modules.BassService.HealthChecks;
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
            services.Configure<TagReaderConfig>(configuration.GetSection(TagReaderConfig.Section));

            services.AddSingleton<IBassWrapper, BassWrapper>();
            services.AddSingleton<ITagReader, TagReader>(); 
            
            services.AddHostedService<BassServiceHost>();

            services.AddHttpClient<StreamingServerHealthCheck>();
            services.AddHealthChecks().AddCheck<StreamingServerHealthCheck>("StreamingServer");
        }
    }
}
