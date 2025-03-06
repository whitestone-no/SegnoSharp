using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Modules.TagReader.Helpers;
using Whitestone.SegnoSharp.Modules.TagReader.Interfaces;
using Whitestone.SegnoSharp.Modules.TagReader.Models.Config;

namespace Whitestone.SegnoSharp.Modules.TagReader
{
    public class Module : IModule
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
            services.Configure<BassRegistration>(configuration.GetSection(BassRegistration.Section));

            services.AddSingleton<IBassWrapper, BassWrapper>();
            services.AddSingleton<ITagReader, TagReader>(); 
        }
    }
}
