using Microsoft.Extensions.DependencyInjection;
using Whitestone.SegnoSharp.PersistenceManager.Interfaces;

namespace Whitestone.SegnoSharp.PersistenceManager.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistenceManager(this IServiceCollection services)
        {
            services.AddSingleton<IPersistenceManager, PersistenceHandler>();
            services.AddHostedService(p => p.GetRequiredService<IPersistenceManager>());

            return services;
        }
    }
}
