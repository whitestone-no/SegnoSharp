using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Whitestone.SegnoSharp.Common.Helpers;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Common.Models.Persistent;

namespace Whitestone.SegnoSharp.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCommon(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TagReaderConfig>(configuration.GetSection(TagReaderConfig.Section));

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
