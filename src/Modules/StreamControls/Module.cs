using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Modules.StreamControls.HealthChecks;
using Whitestone.SegnoSharp.Modules.StreamControls.Models;

namespace Whitestone.SegnoSharp.Modules.StreamControls
{
    public class Module : IModule
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
            services.AddSingleton(sp =>
            {
                Settings settings = new();
                var persistenceManager = sp.GetRequiredService<IPersistenceManager>();
                persistenceManager.RegisterAsync(settings);
                return settings;
            });

            services.AddHttpClient<StreamingServerHealthCheck>();
            services.AddHealthChecks().AddCheck<StreamingServerHealthCheck>("StreamingServer");

        }
    }
}
