using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Whitestone.SegnoSharp.Modules.Dashboard.Models;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Modules.Dashboard
{
    public class Module : IModule
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
            services.AddSingleton(sp =>
            {
                DashboardSettings settings = new();
                var pm = sp.GetRequiredService<IPersistenceManager>();
                pm.Register(settings);
                return settings;
            });
        }
    }
}
