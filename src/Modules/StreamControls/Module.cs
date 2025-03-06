using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Modules.StreamControls.HealthChecks;

namespace Whitestone.SegnoSharp.Modules.StreamControls
{
    public class Module : IModule
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
            services.AddHttpClient<StreamingServerHealthCheck>();
            services.AddHealthChecks().AddCheck<StreamingServerHealthCheck>("StreamingServer");

        }
    }
}
