using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models;

namespace Whitestone.SegnoSharp.Modules.MediaImporter
{
    public class Module : IModule
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
            services.AddScoped<ImportState>();
        }
    }
}
