using Microsoft.Extensions.DependencyInjection;
using Whitestone.SegnoSharp.Common.Helpers;
using Whitestone.SegnoSharp.Common.Interfaces;

namespace Whitestone.SegnoSharp.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCommon(this IServiceCollection services)
        {
            services.AddTransient<ISystemClock, SystemClock>();

            services.AddSingleton<IPersistenceManager, PersistenceHandler>();
            services.AddHostedService(p => p.GetRequiredService<IPersistenceManager>());

            return services;
        }
    }
}
