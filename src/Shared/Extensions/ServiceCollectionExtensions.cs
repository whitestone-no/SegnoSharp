using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Whitestone.SegnoSharp.Shared.Helpers;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Persistent;

namespace Whitestone.SegnoSharp.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCommon(this IServiceCollection services)
        {
            services.AddTransient<ISystemClock, SystemClock>();
            services.AddTransient<IRandomGenerator, RandomGenerator>();
            services.AddTransient<IHashingUtil, HashingUtil>();

            services.AddSingleton<IPersistenceManager, PersistenceHandler>();
            services.AddHostedService(p => p.GetRequiredService<IPersistenceManager>());

            services.AddSingleton(sp =>
            {
                AudioSettings settings = new();
                var persistence = sp.GetRequiredService<IPersistenceManager>();
                persistence.RegisterAsync(settings);
                return settings;
            });

            services.AddSingleton(sp =>
            {
                StreamingSettings settings = new();
                var persistence = sp.GetRequiredService<IPersistenceManager>();
                persistence.RegisterAsync(settings);
                return settings;
            });

            return services;
        }
    }
}
